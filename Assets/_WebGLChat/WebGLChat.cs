using System.Collections;
using System.Collections.Generic;
using LiteNetLib;
using UnityEngine;
using UnityEngine.UI;

public class WebGLChat : MonoBehaviour,ILNSManagerDataReceiver
{
    // Start is called before the first frame update
    public ScrollRect scrollRect;
    public TMPro.TMP_InputField field;
    public TextAsset animalNamesDB;
    public ChatElement chatElementPrefab;
    private List<string> animals = new List<string>();
    void Start()
    {
        animals.AddRange(animalNamesDB.text.Split('\n'));
        chatElementPrefab.gameObject.SetActive(false);

        if(!PlayerPrefs.HasKey("id"))
        {
            PlayerPrefs.SetString("id", System.DateTime.Now.ToLongTimeString() + Random.Range(100000, 999999));
            PlayerPrefs.Save();
        }


        LNSClientParameters clientParameters = new LNSClientParameters(PlayerPrefs.GetString("id"), RandomAnimalName());
       
        LNSConnectSettings connectSettings = new LNSConnectSettings();
        connectSettings.serverIp = "vps.hybriona.com";
        connectSettings.serverPort = 10002;
        connectSettings.serverSecurityKey = "iamatestserver";// ServerKey.GetKey();
        connectSettings.gameKey = "com.hybriona.webgl_chat";
        connectSettings.Validate();

        LNSManager.Initialize(clientParameters, connectSettings, this);

        LNSManager.connector.onConnected = () =>
        {
            AddMsg("System", "Connected to server");
            LNSManager.connector.JoinRoomOrCreateIfNotExist("default");
        };

        LNSManager.connector.onFailedToConnect = (error) =>
        {
            AddMsg("System", "Failed to connect "+ error);
        };

        LNSManager.connector.onRoomCreated = OnRoomActive;
        LNSManager.connector.onRoomJoined = OnRoomActive;

        field.onEndEdit.AddListener(OnSendMsg);

        LNSManager.Connect();

    }

    public void OnSendMsg(string msg)
    {
        if(!string.IsNullOrEmpty(msg))
        {
            scrollRect.verticalNormalizedPosition = 0;
            LNSManager.DATA_WRITER.Reset();
            LNSManager.DATA_WRITER.Put(msg);
            LNSManager.RaiseEventOnAll(11, LNSManager.DATA_WRITER, DeliveryMethod.ReliableOrdered);

            AddMsg(LNSManager.connector.localClient.displayName, msg);
            scrollRect.verticalNormalizedPosition = 0;
            field.text = "";
        }
    }

    public void AddMsg(string username,string msg)
    {
        chatElementPrefab.Fill(username, msg);
    }

    private string RandomAnimalName()
    {
        string animName = animals[Random.Range(0, animals.Count)];
        if(animName.Contains(" "))
        {
            return RandomAnimalName();
        }
        return animName;
    }

    private void OnRoomActive()
    {
        AddMsg("System", "In Active room");
    }

    void ILNSManagerDataReceiver.OnEventRaised(LNSClient from, ushort eventCode, LNSReader reader, DeliveryMethod deliveryMethod)
    {
        if(eventCode == 11)
        {
            AddMsg(from.displayName, reader.GetString());
            scrollRect.verticalNormalizedPosition = 0;
        }
    }

    void ILNSManagerDataReceiver.OnCachedDataReceived(string ofKey, byte[] rawData)
    {
       
    }

    private void OnDisable()
    {
        LNSManager.connector.Disconnect();
    }
}
