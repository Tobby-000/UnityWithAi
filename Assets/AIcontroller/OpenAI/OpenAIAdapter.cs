/*************************************************
 * 
 * 
 * 
 *      ��OpenAI�ӿ�������
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


    public override void SendRequest()               //���ݲ�����������
    {
        // ����״̬
        fullResponseBuilder.Clear();
        inThinkBlock = false;
        responseText = "";
        StartCoroutine(SendRequestCoroutine());//����Э��
    }
    // ��������Э��
    private IEnumerator SendRequestCoroutine()
    {
        string url = baseUrl + "/v1/completions";
        string json = null;
        //�����Ƿ�ʹ��https����url
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
            Debug.Log("����URL: " + url);
            Debug.Log("����JSON: " + json);
        }
        byte[] byteArray = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))//UnityWebRequest  Post����
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
                Debug.LogError("����: " + request.error);
                this.responseText = "����: " + request.error;
                OnResponseUpdated?.Invoke(this.responseText);
            }
            else if (!stream)
            {
                OpenGetJson get = JsonUtility.FromJson<OpenGetJson>(request.downloadHandler.text);
                ProcessResponse(get.choices[0].message.content);
            }
        }
    }

    // ��������Ӧ��
    private void ProcessResponseChunk(string chunkResponse)
    {
        if (ignorethink)
        {
            // ����<think>��
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
                return; // ����<think>���ڵ�����
            }
        }
        fullResponseBuilder.Append(chunkResponse);
        responseText = fullResponseBuilder.ToString();
        OnResponseUpdated?.Invoke(responseText);
        return;
    }

    // ����������Ӧ������ʽ��
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

    // ��ʽ���ش�����
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

                // ����δ����Ĳ���
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
                            Debug.Log("�յ����ݿ�: " + delta.content);
                        }
                        controller.ProcessResponseChunk(delta.content);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("JSON��������: " + e.Message + "\n����: " + jsonContent);
            }
        }

        protected override void CompleteContent()
        {
            // ����ʣ������
            ProcessBuffer();
        }
    }
    
}

