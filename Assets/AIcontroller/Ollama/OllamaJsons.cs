/*****************************
 *	
 *	Ollama模式
 *	
 *	
 *	Json序列化及反序列化类
 *	
 *	带chat为聊天模式
 *	不带chat为生成模式
 *	
 *	
 *****************************/
using System;
using UnityEngine;

//共用Message以及Options
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


//chat模式返回Json
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


//chat模式发送Json
public class ChatPutJson: OllamaJsons
{
	public string model;
	public ChatMessage[] messages;
	public bool stream;
	public ChatOptions options;
}

//Generate模式发送Json
public class PutJson: OllamaJsons
{
	public string model;
	public string prompt;
	public bool stream;
	public ChatOptions options;
}

//Generate模式返回Json
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