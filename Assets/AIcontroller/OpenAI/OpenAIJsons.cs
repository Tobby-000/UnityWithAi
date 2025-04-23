using System;
using UnityEngine;


[Serializable]
public class OpenMessageJson 
{
    public string role;
    public string context;
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
    public OpenMessageJson message;
    public string finish_reason;
    public int index;
}
public class OpenPutJson
{
    public string model;
    public OpenMessageJson[] messages;
    public float temperature;
}
public class OpenGetJson
{
    public string id;
    public string @object;
    public int created;
    public string model;
    public OpenUsageJson usage;
    public OpenChoiceJson[] choices;
}