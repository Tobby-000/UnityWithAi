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
    public void OnClick()
    {
        var intmp = input.GetComponent<TextMeshProUGUI>();
        string intext = intmp.text;
        var aicontroll = GetComponent<AIController>();
        aicontroll.content = intext;
        aicontroll.Chat();
        output.text = aicontroll.responseText;
        return;
        
    }
    public void Update()
    {

        var aicontroll = GetComponent<AIController>();
        output.text = aicontroll.responseText;
    }
}
