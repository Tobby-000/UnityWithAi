/******************************************
 *
 *      ����������
 *
 ******************************************/

using UnityEngine;
public class Adapter : MonoBehaviour
{
    //���޸Ĳ���
    //�������
    public string model;                    //ģ������
    public string baseUrl;                  //API URL
    //����ģ�ͱ������
    public string apiKey;                   //API��Կ
    public Emode mode;                      //ģʽѡ��
    //��ѡ����
    public bool stream = false;             //�Ƿ���ʽ���
    public bool ignorethink = true;         //�Ƿ����think��
    public float temperature = 0.7f;        //�¶�
    public int max_tokens = 1000;           //���token��
    public bool debug = false;              //����ģʽ,�Ƿ��ӡ�������Ӧ��Ϣ

    //�����޸Ĳ���,����
    public string content;                  //��ʾ��
    public string responseText;             //��Ӧ�ı�,�����޸�

    public enum Emode
    {
        Generate,
        Chat
    }
    public virtual void SendRequest() { }
}