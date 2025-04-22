/*************************************************
 * 
 * 
 * 
 *      Ollama接口控制器
 *      目前支持Generate和Chat两种模式
 *      可选参数：
 *      流式输出，温度，忽略think块，max_tokens
 *      
 *      使用方法：
 *      首先本地安装Ollama
 *      如果是本地安装的ollama，无需修改baseUrl
 *      
 *      
 *      
 **************************************************/
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;


public class OllamaContorller : MonoBehaviour
{
    public enum Emode{
        Generate,
        Chat
    }

    //可修改参数
    public string model;                    //模型名称
    public string baseUrl="127.0.0.1:11434";//本地安装的ollama
    public bool stream = false;             //是否流式输出
    public bool ignorethink = true;         //是否忽略think块
    public Emode mode;                      //模式选择
    public float temperature=0.7f;          //温度
    public int max_tokens = 1000;           //最大token数

    public bool debug = false;               //调试模式,是否打印请求和响应信息
    //无需修改参数,调用
    public string prompt;                   //提示词
    public string responseText;             //响应文本,无需修改

    public event Action<string> OnResponseUpdated;
    private StringBuilder fullResponseBuilder = new StringBuilder();
    private bool inThinkBlock = false;

    
    public void SendRequest()               //根据参数发送请求
    {
        // 重置状态
        fullResponseBuilder.Clear();
        inThinkBlock = false;
        responseText = "";
        StartCoroutine(SendRequestCoroutine());//启动协程
    }
    // 发送请求协程
    private IEnumerator SendRequestCoroutine()
    {
        string url=null;
        string json = null;
        //生成模式
        if (mode == Emode.Generate) 
        { 
            url = "http://" + baseUrl + "/api/generate";
            var putJson = new PutJson
            {
                model = this.model,
                prompt = this.prompt,
                stream = this.stream,
                options = new ChatOptions
                {
                    max_tokens = 1000,
                    temperature = this.temperature
                }
            };
            json = JsonUtility.ToJson(putJson);//将数据转换为JSON格式
        }
        else if(mode == Emode.Chat) {
            url = "http://" + baseUrl + "/api/chat";
            var chatputjson = new ChatPutJson
            {
                model = this.model,
                messages = new ChatMessage[]
                {
                    new ChatMessage
                    {
                        role = "user",
                        content = this.prompt
                    }
                },
                stream = this.stream,
                options = new ChatOptions
                {
                    max_tokens = 1000,
                    temperature = this.temperature
                },
            };
            json = JsonUtility.ToJson(chatputjson);
        }
        if(debug)
        {
            Debug.Log("请求URL: " + url);
            Debug.Log("请求JSON: " + json);
        }
        byte[] byteArray = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))//UnityWebRequest  Post请求
        {
            request.uploadHandler = new UploadHandlerRaw(byteArray);

            if (stream)
            {
                request.downloadHandler = new StreamingDownloadHandler(this,mode);
            }
            else
            {
                request.downloadHandler = new DownloadHandlerBuffer();
            }

            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("错误: " + request.error);
                this.responseText = "错误: " + request.error;
                OnResponseUpdated?.Invoke(this.responseText);
            }
            else if (!stream)
            {
                GetJson get = JsonUtility.FromJson<GetJson>(request.downloadHandler.text);
                ProcessResponse(get.response);
            }
        }
    }

    // 处理单个响应块
    public void ProcessResponseChunk(string chunkResponse)
    {
        if (ignorethink)
        {
            // 处理<think>块
            if (chunkResponse == "<think>")
            {
                inThinkBlock = true;
                return;
            }
            else if (chunkResponse == "</think>")
            {
                inThinkBlock = false;
                return;
            }
            else if (inThinkBlock)
            {
                return; // 忽略<think>块内的内容
            }
        }

        // 累积响应
        fullResponseBuilder.Append(chunkResponse);
        responseText = fullResponseBuilder.ToString();
        OnResponseUpdated?.Invoke(responseText);
    }

    // 处理完整响应（非流式）
    private void ProcessResponse(string response)
    {
        if (ignorethink)
        {
            int endofthink = response.IndexOf("</think>\n\n") + 10;
            if (endofthink >= 10)
            {
                responseText = response[endofthink..];
            }
            else
            {
                responseText = response;
            }
        }
        else
        {
            responseText = response;
        }
        OnResponseUpdated?.Invoke(responseText);
    }

    // 流式下载处理器
    private class StreamingDownloadHandler : DownloadHandlerScript
    {
        private OllamaContorller controller;
        private StringBuilder chunkBuilder = new StringBuilder();
        private Emode mode = Emode.Generate;

        public StreamingDownloadHandler(OllamaContorller controller,Emode mode) : base(new byte[2048])
        {
            this.controller = controller;
            this.mode = mode;
        }

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || dataLength == 0)
                return false;

            string chunk = Encoding.UTF8.GetString(data, 0, dataLength);
            chunkBuilder.Append(chunk);

            // 处理可能的多条JSON响应（API返回逐行JSON）
            string[] lines = chunkBuilder.ToString().Split('\n');
            for (int i = 0; i < lines.Length - 1; i++) // 最后一行可能不完整
            {
                string line = lines[i].Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    try
                    {
                        if (mode == Emode.Generate)
                        {
                            var response = JsonUtility.FromJson<GetJson>(line);
                            if (response != null && !string.IsNullOrEmpty(response.response))
                            {
                                controller.ProcessResponseChunk(response.response);
                                if(controller.debug)
                                    // 仅在调试模式下输出
                                    Debug.Log("处理响应: " + response.response); // 调试输出
                            }
                        }
                        else if(mode == Emode.Chat)
                        {
                            var responsechat = JsonUtility.FromJson<ChatGetJson>(line);
                            if (responsechat != null && !string.IsNullOrEmpty(responsechat.message.content))
                            {
                                controller.ProcessResponseChunk(responsechat.message.content);
                                if (controller.debug)
                                    Debug.Log("处理响应: " + responsechat.message.content);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("JSON解析错误: " + e.Message);
                    }
                }
            }

            // 保存最后不完整的行
            if (lines.Length > 0)
                chunkBuilder = new StringBuilder(lines[lines.Length - 1]);

            return true;
        }

        protected override void CompleteContent()
        {
            // 处理剩余内容
            string remaining = chunkBuilder.ToString().Trim();
            if (!string.IsNullOrEmpty(remaining))
            {
                try
                {
                    if (mode == Emode.Generate)
                    {
                        var response = JsonUtility.FromJson<GetJson>(remaining);
                        if (response != null && !string.IsNullOrEmpty(response.response))
                        {
                            controller.ProcessResponseChunk(response.response);
                            if (controller.debug)
                                Debug.Log("处理响应: " + response.response);
                        }
                    }
                    else if (mode == Emode.Chat)
                    {
                        var responsechat = JsonUtility.FromJson<ChatGetJson>(remaining);
                        if (responsechat != null && !string.IsNullOrEmpty(responsechat.message.content))
                        {
                            controller.ProcessResponseChunk(responsechat.message.content);
                            if (controller.debug)
                                Debug.Log("处理响应: " + responsechat.message.content);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("最终JSON解析错误: " + e.Message);
                }
            }
        }
    }
}
