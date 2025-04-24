# Unity With AI

你是否考虑将ai接入到你的unity？


试试这个简易的解决方案


源文件中aibuttonsend为样例代码

-----
## 具体使用方法

- 现将AIContorller脚本拖进物品
- 将你想用的适配器拖进物品里面
- 将控制器必要参数写好，比如baseUrl="http://127.0.0.1/11434"即可
- 因为在控制器里面用的是协程，所以你不能立即获得responseText，所以有两种方案：等待生成完，或者直接用update持续获取（这样子你也可以获得流式响应了，就不需要等待生成完再一次输出了）

  
~~~ csharp
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
~~~