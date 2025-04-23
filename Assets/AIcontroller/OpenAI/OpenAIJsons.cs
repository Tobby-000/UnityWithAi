using System;
using UnityEngine;


[Serializable]
public class OpenMessageJson 
{
    public string role;
    public string content;
}
[Serializable]
public class OpenUsageJson
{
    public int prompt_tokens;
    public int completion_tokens;
    public int total_tokens;
}
[Serializable]
public class OpenChoiceJson
{
    public string index;
    public OpenMessageJson message;
    public string logprobs;
    public string finish_reason;
    
}
public class OpenPutJson
{
    public string model;
    public OpenMessageJson[] messages;
    public float temperature;
    public bool stream;
}
public class OpenGetJson
{
    public string id;
    public string @object;
    public int created;
    public string model;
    public OpenUsageJson usage;
    public OpenChoiceJson[] choices;
    public string system_fingerprint;
}
[Serializable]
public class StreamResponse
{
    public StreamChoice[] choices;
}

[Serializable]
public class StreamChoice
{
    public StreamDelta delta;
}

[Serializable]
public class StreamDelta
{
    public string content;
}