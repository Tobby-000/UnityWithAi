# Unity With AI

你是否考虑将ai接入到你的unity？


试试这个简易的解决方案


源文件中aibuttonsend为样例代码

-----
## 具体使用方法

- 将你想用的控制器（或者叫适配器更加恰当）拖进物品里面
- 将控制器必要参数写好，Ollama需要写baseurl格式 ip：端口 即可，OpenAI需要写BaseUrl：一直写到/v1/，以及APIkey
- 因为在控制器里面用的是协程，所以你不能立即获得responseText，所以有两种方案：等待生成完，或者直接用update持续获取（这样子你也可以获得流式响应了，就不需要等待生成完再一次输出了）

  
~~~ csharp
public string outtext=null;
public void onClick(){
  string intext="abab";
  var aicontroll = GetComponent<OllamaController>();
  aicontroll.prompt = intext;
  aicontroll.SendRequest();
}
public void Update(){
  var aicontroll = GetComponent<OllamaController>();
  output.text = aicontroll.responseText;
}
