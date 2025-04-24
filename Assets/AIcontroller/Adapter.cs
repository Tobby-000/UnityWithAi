/******************************************
 *
 *      适配器基类
 *
 ******************************************/

using UnityEngine;
public class Adapter : MonoBehaviour
{
    //可修改参数
    //必须参数
    public string model;                    //模型名称
    public string baseUrl;                  //API URL
    //根据模型必须参数
    public string apiKey;                   //API密钥
    public Emode mode;                      //模式选择
    //可选参数
    public bool stream = false;             //是否流式输出
    public bool ignorethink = true;         //是否忽略think块
    public float temperature = 0.7f;        //温度
    public int max_tokens = 1000;           //最大token数
    public bool debug = false;              //调试模式,是否打印请求和响应信息

    //无需修改参数,调用
    public string content;                  //提示词
    public string responseText;             //响应文本,无需修改

    public enum Emode
    {
        Generate,
        Chat
    }
    public virtual void SendRequest() { }
}