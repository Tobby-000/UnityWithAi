/*****************************
 *	
 *	Ollamaģʽ
 *	
 *	
 *	Json���л��������л���
 *	
 *	��chatΪ����ģʽ
 *	����chatΪ����ģʽ
 *	
 *	
 *****************************/
using System;
using UnityEngine;

//����Message�Լ�Options
[Serializable]
public struct ChatMessage
{
	public string role;
	public string content;
}
[Serializable]
public struct ChatOptions
{
	public float temperature;
	public int max_tokens;
}


//chatģʽ����Json
public class ChatGetJson : OllamaJsons
{
	public string model;
	public DateTimeOffset created_at;
	public ChatMessage message;
	public bool done;
	public string done_reason;
	public string context;
	public string total_duration;
	public string load_duration;
	public string prompt_eval_count;
	public string prompt_eval_duration;
	public string eval_count;
	public string eval_duration;
}


//chatģʽ����Json
public class ChatPutJson: OllamaJsons
{
	public string model;
	public ChatMessage[] messages;
	public bool stream;
	public ChatOptions options;
}

//Generateģʽ����Json
public class PutJson: OllamaJsons
{
	public string model;
	public string prompt;
	public bool stream;
	public ChatOptions options;
}

//Generateģʽ����Json
public class GetJson : OllamaJsons
{
    public string model;
    public DateTimeOffset created_at;
    public string response;
    public bool done;
    public string done_reason;
    public string context;
    public string total_duration;
    public string load_duration;
    public string prompt_eval_count;
    public string prompt_eval_duration;
    public string eval_count;
    public string eval_duration;

}

public class OllamaJsons
{
	public virtual string ToJson()
	{
        return JsonUtility.ToJson(this);
    }
}