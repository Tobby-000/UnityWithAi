using System;
using UnityEditor;
using UnityEngine;

public class AIController:MonoBehaviour
{
    public enum AdapterType
    {
        Ollama,
        OpenAI
    }
    public AdapterType adapterType;
    public string content;
    public string responseText;
    public void Start()
    {
        //bool isAdapter= false;
        //ºÏ≤‚  ≈‰∆˜
        if (adapterType == AdapterType.Ollama)
        {
            var adapter = GetComponent<OllamaAdapter>();
            if (adapter == null)
            {
                Debug.LogError("OllamaAdapter not found!");
                //isAdapter = true;
            }
            else
                return;
        }
        else if (adapterType == AdapterType.OpenAI)
        {
            var adapter = GetComponent<OpenAIAdapter>();
            if (adapter == null)
            {
                Debug.LogError("OpenAIAdapter not found!");
                //isAdapter = true;
            }
            else
                return;
        }
        //◊‘∂ØºÏ≤‚  ≈‰∆˜
        var adapterOPEN = GetComponent<OpenAIAdapter>();
        if (adapterOPEN != null)
        {
            Debug.Log("OpenAIAdapter found!");
            adapterType = AdapterType.OpenAI;
        }
        var adapterOLLA = GetComponent<OllamaAdapter>();
        if (adapterOLLA != null)
        {
            Debug.Log("OllamaAdapter found!");
            adapterType = AdapterType.Ollama;
        }
        else
        {
            Debug.LogError("No adapter found!");
            return;
        }
    }

    public void Chat()
    {
        if(adapterType == AdapterType.Ollama)
        {
            var adapter = GetComponent<OllamaAdapter>();
            adapter.content = content;
            adapter.SendRequest();
            responseText = adapter.responseText;
        }
        else if(adapterType == AdapterType.OpenAI)
        {
            var adapter = GetComponent<OpenAIAdapter>();
            adapter.content = content;
            adapter.SendRequest();
            responseText = adapter.responseText;
        }
    }
    public void Update()
    {
        if (adapterType == AdapterType.Ollama)
        {
            var adapter = GetComponent<OllamaAdapter>();
            responseText = adapter.responseText;
        }
        else if (adapterType == AdapterType.OpenAI)
        {
            var adapter = GetComponent<OpenAIAdapter>();
            responseText = adapter.responseText;
        }
    }
}