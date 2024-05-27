using System.Collections;
using System.Collections.Generic;
using LiteNetLib;
using UnityEngine;

public class FakeClient : ILNSDataReceiver
{
    public System.Action<string> messageReceiver;
    public string id
    {
        get { return connector.localClient.id; }
    }
    public bool isConnected {
        get { return connector.isConnected && connector.isInActiveRoom; }
    }

    private LNSConnector connector;
    private LNSWriter writer;
    public FakeClient(string ip,int port,string id)
    {
        LNSClientParameters clientParameters = new LNSClientParameters(id, null);
        LNSConnectSettings connectSettings = new LNSConnectSettings();
        connectSettings.serverIp = ip;
        connectSettings.serverPort = port;
        connectSettings.serverSecurityKey = ServerKey.GetKey();
        connectSettings.gameKey = "hybriona.ccutest";

        writer = new LNSWriter();
        connector = new LNSConnector(clientParameters, connectSettings,this);

        connector.onConnected += () =>
        {
            connector.JoinRoomOrCreateIfNotExist("test", 10000);
        };

        if (id.Contains("_10"))
        {


            connector.onPlayerConnected += (cClient) =>
            {
                
                Debug.Log(cClient.id + " connected");
            };
        }
        connector.Connect();
    }

    public void SendData()
    {
        //Debug.Log("connector.isConnected : " + connector.isConnected + " | connector.isInActiveRoom: " + connector.isInActiveRoom);
        writer.Reset();
        writer.Put("Message from " + id + " at " +  System.DateTime.Now.ToString());
        connector.RaiseEventOnAll(15, writer, DeliveryMethod.ReliableOrdered);
    }

    public void OnEventRaised(LNSClient from, ushort eventCode, LNSReader reader, DeliveryMethod deliveryMethod)
    {
        if(messageReceiver != null)
        {
            if(eventCode != 15)
            {
                Debug.Log("Wrong event code "+eventCode);
            }
            
            //Debug.Log(reader.AvailableBytes + " "+reader.PeekUShort());
            messageReceiver( reader.GetString());
        }
    }

    public void Disconnect()
    {
        if (connector != null)
        {
            connector.Disconnect();
        }
    }

    public void OnCachedDataReceived(string key, byte [] rawData)
    {
        
    }
}
