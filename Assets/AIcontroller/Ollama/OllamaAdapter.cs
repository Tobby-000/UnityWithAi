/*************************************************
 * 
 * 
 * 
 *      Ollama�ӿ�������
 *      Ŀǰ֧��Generate��Chat����ģʽ
 *      ��ѡ������
 *      ��ʽ������¶ȣ�����think�飬max_tokens
 *      
 *      ʹ�÷�����
 *      ���ȱ��ذ�װOllama
 *      ����Ǳ��ذ�װ��ollama�������޸�baseUrl
 *      
 *      
 *      
 **************************************************/
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;


public class OllamaAdapter : Adapter
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
        string url=null;
        string json = null;
        //string protocol = ishttps ? "https://" : "http://";
        //����ģʽ
        if (mode == Emode.Generate) 
        {
            url = baseUrl + "/api/generate";
            var putJson = new PutJson
            {
                model = this.model,
                prompt = this.content,
                stream = this.stream,
                options = new ChatOptions
                {
                    max_tokens = 1000,
                    temperature = this.temperature
                }
            };
            json = JsonUtility.ToJson(putJson);//������ת��ΪJSON��ʽ
        }
        else if(mode == Emode.Chat) {
            url = baseUrl + "/api/chat";
            var chatputjson = new ChatPutJson
            {
                model = this.model,
                messages = new ChatMessage[]
                {
                    new ChatMessage
                    {
                        role = "user",
                        content = this.content
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
            Debug.Log("����URL: " + url);
            Debug.Log("����JSON: " + json);
        }
        byte[] byteArray = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))//UnityWebRequest  Post����
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
                Debug.LogError("����: " + request.error);
                this.responseText = "����: " + request.error;
                OnResponseUpdated?.Invoke(this.responseText);
            }
            else if (!stream)
            {
                GetJson get = JsonUtility.FromJson<GetJson>(request.downloadHandler.text);
                ProcessResponse(get.response);
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

        // �ۻ���Ӧ
        fullResponseBuilder.Append(chunkResponse);
        responseText = fullResponseBuilder.ToString();
        OnResponseUpdated?.Invoke(responseText);
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
        private OllamaAdapter controller;
        private StringBuilder chunkBuilder = new StringBuilder();
        private Emode mode = Emode.Generate;

        public StreamingDownloadHandler(OllamaAdapter controller,Emode mode) : base(new byte[2048])
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

            // ������ܵĶ���JSON��Ӧ��API��������JSON��
            string[] lines = chunkBuilder.ToString().Split('\n');
            for (int i = 0; i < lines.Length - 1; i++) // ���һ�п��ܲ�����
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
                                    // ���ڵ���ģʽ�����
                                    Debug.Log("������Ӧ: " + response.response); // �������
                            }
                        }
                        else if(mode == Emode.Chat)
                        {
                            var responsechat = JsonUtility.FromJson<ChatGetJson>(line);
                            if (responsechat != null && !string.IsNullOrEmpty(responsechat.message.content))
                            {
                                controller.ProcessResponseChunk(responsechat.message.content);
                                if (controller.debug)
                                    Debug.Log("������Ӧ: " + responsechat.message.content);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("JSON��������: " + e.Message);
                    }
                }
            }

            // ���������������
            if (lines.Length > 0)
                chunkBuilder = new StringBuilder(lines[lines.Length - 1]);

            return true;
        }

        protected override void CompleteContent()
        {
            // ����ʣ������
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
                                Debug.Log("������Ӧ: " + response.response);
                        }
                    }
                    else if (mode == Emode.Chat)
                    {
                        var responsechat = JsonUtility.FromJson<ChatGetJson>(remaining);
                        if (responsechat != null && !string.IsNullOrEmpty(responsechat.message.content))
                        {
                            controller.ProcessResponseChunk(responsechat.message.content);
                            if (controller.debug)
                                Debug.Log("������Ӧ: " + responsechat.message.content);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("����JSON��������: " + e.Message);
                }
            }
        }
    }
}
