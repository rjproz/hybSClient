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
        connectSettings.serverSecurityKey = "iamatestserver";
        connectSettings.gameKey = "hybriona.ccutest";

        writer = new LNSWriter();
        connector = new LNSConnector(clientParameters, connectSettings,this);

        connector.onConnected += () =>
        {
            connector.JoinRoomOrCreateIfNotExist("test", 10000);
        };

        connector.Connect();
    }

    public void SendData()
    {
        writer.Reset();
        writer.Put("Message from " + id + " at "+ System.DateTime.Now.ToFileTime());
        connector.RaiseEventOnAll(0, writer, DeliveryMethod.ReliableOrdered);
    }

    public void OnEventRaised(LNSClient from, ushort eventCode, LNSReader reader, DeliveryMethod deliveryMethod)
    {
        if(messageReceiver != null)
        {
            messageReceiver(reader.GetString());
        }
    }
}
