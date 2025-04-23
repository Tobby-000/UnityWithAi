/*************************************************
 * 
 * 
 * 
 *      ��OpenAI�ӿڿ�����
 *      ��δ���в���
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

    //���޸Ĳ���
    public string model= "gpt-3.5-turbo";   //ģ������
    public string baseUrl = "https://api.openai.com/v1/completions";//API URL
    public string apikey = "";              //API��Կ
    public bool stream = false;             //�Ƿ���ʽ���
    public bool ignorethink = true;         //�Ƿ����think��
    public float temperature = 0.7f;        //�¶�
    public int max_tokens = 1000;           //���token��
    public bool debug = false;              //����ģʽ,�Ƿ��ӡ�������Ӧ��Ϣ
    //�����޸Ĳ���,����
    public string context;                  //��ʾ��
    public string responseText;             //��Ӧ�ı�,�����޸�

    public event Action<string> OnResponseUpdated;
    private StringBuilder fullResponseBuilder = new StringBuilder();
    private bool inThinkBlock = false;


    public void SendRequest()               //���ݲ�����������
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
        string url = null;
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
                    context=this.context
                }
            }
        };
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
            request.SetRequestHeader("Authorization","Bearer "+apikey);

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
    public void ProcessResponseChunk(string chunkResponse)
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

            // ������ܵĶ���JSON��Ӧ��API��������JSON��
            string[] lines = chunkBuilder.ToString().Split('\n');
            for (int i = 0; i < lines.Length - 1; i++) // ���һ�п��ܲ�����
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
                            // ���ڵ���ģʽ�����
                            Debug.Log("������Ӧ: " + response.response); // �������
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
                   
                        var response = JsonUtility.FromJson<GetJson>(remaining);
                        if (response != null && !string.IsNullOrEmpty(response.response))
                        {
                            controller.ProcessResponseChunk(response.response);
                            if (controller.debug)
                                Debug.Log("������Ӧ: " + response.response);
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
