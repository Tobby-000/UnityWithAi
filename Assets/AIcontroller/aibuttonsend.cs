using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

public class aibuttonsend : MonoBehaviour
{
    public TextMeshProUGUI input;
    public TextMeshProUGUI output;
    public enum Controller
    {
        Ollama,
        OpenAI
    }
    public Controller controller;
    public void OnClick()
    {
        var intmp = input.GetComponent<TextMeshProUGUI>();
        string intext = intmp.text;
        if (controller == Controller.Ollama)
        {
            var aicontroll = GetComponent<OllamaController>();
            aicontroll.prompt = intext;
            aicontroll.SendRequest();
            output.text = aicontroll.responseText;
            return;
        }
        else if (controller == Controller.OpenAI)
        {
            var aicontroll = GetComponent<OpenAIController>();
            aicontroll.content = intext;
            aicontroll.SendRequest();
            output.text = aicontroll.responseText;
            return;
        }
    }
    public void Update()
    {
        if (controller == Controller.Ollama)
        {
            var aicontroll = GetComponent<OllamaController>();
            output.text = aicontroll.responseText;
        }
        else if (controller == Controller.OpenAI)
        {
            var aicontroll = GetComponent<OpenAIController>();
            output.text = aicontroll.responseText;
            }
        }
}
