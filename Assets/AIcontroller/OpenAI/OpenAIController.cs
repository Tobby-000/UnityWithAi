/*************************************************
 * 
 * 
 * 
 *      类OpenAI接口控制器
 *      尚未进行测试
 *      
 *      
 **************************************************/
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using UnityEditor.MPE;


public class OpenAIController : MonoBehaviour
{

    //可修改参数
    public string model= "gpt-3.5-turbo";   //模型名称
    public string baseUrl = "https://api.openai.com/v1/completions";//API URL
    public string apikey = "";              //API密钥
    public bool stream = false;             //是否流式输出
    public bool ignorethink = true;         //是否忽略think块
    public float temperature = 0.7f;        //温度
    public int max_tokens = 1000;           //最大token数
    public bool debug = false;              //调试模式,是否打印请求和响应信息
    //无需修改参数,调用
    public string context;                  //提示词
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
        string url = null;
        string json = null;
        //根据是否使用https设置url
        OpenPutJson openputjson = new OpenPutJson()
        {
            model = this.model,
            messages = new OpenMessageJson[]
            {
                new OpenMessageJson
                {
                    role="user",
                    context=this.context
                }
            }
        };
        if (debug)
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
                request.downloadHandler = new StreamingDownloadHandler(this);
            }
            else
            {
                request.downloadHandler = new DownloadHandlerBuffer();
            }

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization","Bearer "+apikey);

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
        private OpenAIController controller;
        private StringBuilder chunkBuilder = new StringBuilder();

        public StreamingDownloadHandler(OpenAIController controller) : base(new byte[2048])
        {
            this.controller = controller;
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
                        var response = JsonUtility.FromJson<GetJson>(line);
                        if (response != null && !string.IsNullOrEmpty(response.response))
                        {
                            controller.ProcessResponseChunk(response.response);
                            if (controller.debug)
                            // 仅在调试模式下输出
                            Debug.Log("处理响应: " + response.response); // 调试输出
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
                   
                        var response = JsonUtility.FromJson<GetJson>(remaining);
                        if (response != null && !string.IsNullOrEmpty(response.response))
                        {
                            controller.ProcessResponseChunk(response.response);
                            if (controller.debug)
                                Debug.Log("处理响应: " + response.response);
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
