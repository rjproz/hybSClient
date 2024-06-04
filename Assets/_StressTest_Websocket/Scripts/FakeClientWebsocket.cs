using System;
using System.Collections;
using System.Collections.Generic;
using Mirror.SimpleWeb;
using UnityEngine;

public class FakeClientWebsocket 
{
    public System.Action<string> messageReceiver;
    public int id;

    private LNSWriter writer;
    private SimpleWebClient client;
    public bool isConnected { get; private set; }
    public FakeClientWebsocket(int id,string ip,int port)
    {
        writer = new LNSWriter();
        this.id = id;
        string url = "";
        if(ip.Contains("localhost"))
        {
            url = $"ws://{ip}:{port}";
        }
        else
        {
            url = $"wss://{ip}:{port}";
        }

        var tcpConfig = new TcpConfig(true, 0, 0);


        client = SimpleWebClient.Create(16 * 1024, 3000, tcpConfig);


        client.onConnect += Client_onConnect;
        client.onData += Client_onData;
        client.onDisconnect += Client_onDisconnect;
        client.onError += Client_onError;

        client.Connect(new System.Uri(url));

        
    }

    public void Update()
    {
        if(client != null)
        {
            client.ProcessMessageQueue();
        }
    }

    private void Client_onError(Exception obj)
    {
        Debug.LogError(obj);
    }

    public void Disconnect()
    {
        if(isConnected)
        {
            client.Disconnect();
        }
    }
    private void Client_onDisconnect()
    {
        Debug.Log("On Disconnected " + id);
        isConnected = false;
    }

    
    private void Client_onData(System.ArraySegment<byte> data)
    {
        if (messageReceiver != null)
        {
            var reader = LNSReader.GetFromPool();
            reader.SetSource(data.Array, 0, data.Count);
            string str = reader.GetString();
            reader.Recycle();


            messageReceiver(str );
        }
    }

    private void Client_onConnect()
    {
        Debug.Log("On Connected "+id);
        isConnected = true;
    }

    public void SendData()
    {
        if(isConnected)
        {
            string msg = "Message from " + id + " at " + System.DateTime.Now.ToString();
            writer.Reset();
            writer.Put(msg);
            client.Send(new System.ArraySegment<byte>(writer.Data, 0, writer.Length));
        }
        
        //Debug.Log("connector.isConnected : " + connector.isConnected + " | connector.isInActiveRoom: " + connector.isInActiveRoom);
        
    }

    

    
}
