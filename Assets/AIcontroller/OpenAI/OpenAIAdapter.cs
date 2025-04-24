/*************************************************
 * 
 * 
 * 
 *      类OpenAI接口适配器
 *      
 *      
 *      
 *      
 **************************************************/
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using UnityEditor.MPE;


public class OpenAIAdapter : Adapter
{
    
    public event Action<string> OnResponseUpdated;
    private StringBuilder fullResponseBuilder = new StringBuilder();
    private bool inThinkBlock = false;


    public override void SendRequest()               //根据参数发送请求
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
        string url = baseUrl + "/v1/completions";
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
                    content=this.content
                }
            },
            temperature = this.temperature,
            stream = this.stream
        };
        json = JsonUtility.ToJson(openputjson);
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
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

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
                OpenGetJson get = JsonUtility.FromJson<OpenGetJson>(request.downloadHandler.text);
                ProcessResponse(get.choices[0].message.content);
            }
        }
    }

    // 处理单个响应块
    private void ProcessResponseChunk(string chunkResponse)
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
        fullResponseBuilder.Append(chunkResponse);
        responseText = fullResponseBuilder.ToString();
        OnResponseUpdated?.Invoke(responseText);
        return;
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
        private OpenAIAdapter controller;
        private StringBuilder dataBuffer = new StringBuilder();
        private const string DataPrefix = "data: ";
        private const string DoneMessage = "[DONE]";

        public StreamingDownloadHandler(OpenAIAdapter controller) : base(new byte[1024])
        {
            this.controller = controller;
        }

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || dataLength == 0)
                return false;

            string newData = Encoding.UTF8.GetString(data, 0, dataLength);
            dataBuffer.Append(newData);

            ProcessBuffer();

            return true;
        }

        private void ProcessBuffer()
        {
            string bufferContent = dataBuffer.ToString();
            int lastNewLine = bufferContent.LastIndexOf('\n');

            if (lastNewLine >= 0)
            {
                string[] lines = bufferContent.Substring(0, lastNewLine + 1).Split('\n');

                // 保留未处理的部分
                dataBuffer = new StringBuilder(bufferContent.Substring(lastNewLine + 1));

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string trimmedLine = line.Trim();
                    if (trimmedLine == DoneMessage)
                    {
                        continue;
                        //return;
                    }

                    if (trimmedLine.StartsWith(DataPrefix))
                    {
                        string jsonContent = trimmedLine.Substring(DataPrefix.Length);
                        ProcessJsonContent(jsonContent);
                    }
                }
            }
        }

        private void ProcessJsonContent(string jsonContent)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonContent)) return;

                var response = JsonUtility.FromJson<StreamResponse>(jsonContent);
                if (response != null && response.choices != null && response.choices.Length > 0)
                {
                    var delta = response.choices[0].delta;
                    if (delta != null && !string.IsNullOrEmpty(delta.content))
                    {
                        if (controller.debug)
                        {
                            Debug.Log("收到数据块: " + delta.content);
                        }
                        controller.ProcessResponseChunk(delta.content);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("JSON解析错误: " + e.Message + "\n内容: " + jsonContent);
            }
        }

        protected override void CompleteContent()
        {
            // 处理剩余内容
            ProcessBuffer();
        }
    }
    
}

