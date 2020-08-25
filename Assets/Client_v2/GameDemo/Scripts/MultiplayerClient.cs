﻿using System.Collections;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

public class MultiplayerClient : MonoBehaviour,ILNSDataReceiver
{
    public Transform player;
    public GameObject clonePrefab;
    private Dictionary<string, Clone> others = new Dictionary<string, Clone>();
    private bool active;

    public LNSConnector connector;
    private string id;
    private NetDataWriter writer;

    private Color color;

    public void Initialize(string id)
    {
        player.position = new Vector3(Random.Range(-50, 50), .5f, Random.Range(-50, 50));

        color.r = Random.value;
        color.g = Random.value;
        color.b = Random.value;

        this.id = id +","+color.r + "," + color.g + "," + color.b;

        player.GetComponent<Renderer>().material.color = color;





        LNSConnectSettings settings = new LNSConnectSettings();
        settings.gameKey = "com.hybriona.multiplayertest";
        settings.gameVersion = Application.version;
        settings.serverIp = "45.55.33.88";
        settings.serverPort = 10002;
        settings.serverSecurityKey = "iamatestserver";


        //settings.serverIp = "192.168.0.100";
        connector = new LNSConnector(settings,this);
        connector.id = this.id;
        if (writer == null)
        {
            writer = new NetDataWriter();
        }
        writer.Reset();

        connector.onPlayerConnected = OnPlayerConnected;
       
    }

    public void EventsDoc()
    {   /*

        connector.onConnected += OnConnected;
        connector.onRoomCreated += OnRoomCreated;
        connector.onMasterClientUpdated += OnMasterClientUpdated;
        connector.onRoomCreateFailed += OnRoomCreateFailed;
        connector.onRoomJoined += OnRoomJoined;
        connector.onRoomJoinFailed += OnRoomJoinFailed;
        connector.onPlayerConnected += OnPlayerConnected;
        connector.onPlayerDisconnected += OnPlayerDisconnected;

        connector.onRoomRejoined += OnRoomRejoined ;
        connector.onRoomRejoinFailed += OnRoomRejoinFailed;

        */
    }
    public void Connect()
    {
        connector.Connect();
        
    }

    public void OnDataReceived(LNSClient from, NetPacketReader reader, DeliveryMethod deliveryMethod)
    {

        string playerid = null;
        Vector3 pos = Vector3.zero;
        Quaternion rot = Quaternion.identity;

        playerid = from.id;

        pos.x = reader.GetFloat();
        pos.y = reader.GetFloat();
        pos.z = reader.GetFloat();


        rot.x = reader.GetFloat();
        rot.y = reader.GetFloat();
        rot.z = reader.GetFloat();
        rot.w = reader.GetFloat();

        long timestamp = reader.GetLong();
        float delay =  (float) (System.DateTime.UtcNow -  System.DateTime.FromFileTimeUtc(timestamp)).TotalMilliseconds;
        if(delay > 400)
        {
           
            return;
        }
        if (others.ContainsKey(playerid))
        {
            others[playerid].SetTarget(pos, rot);
        }
    }

    
    private void OnRoomJoinFailed(ROOM_FAILURE_CODE code)
    {
        Debug.Log("OnRoomJoinFailed "+code.ToString());
    }

    private void OnRoomJoined()
    {
        Debug.Log("OnRoomJoined ");
        active = true;
    }

    private void OnMasterClientUpdated(string obj)
    {
        Debug.Log("OnMasterClientUpdated "+obj);
    }

    private void OnPlayerDisconnected(LNSClient client)
    {
        Debug.Log("OnPlayerDisconnected " + client.id);
    }

    private void OnPlayerConnected(LNSClient client)
    {
        Debug.LogFormat("OnPlayerConnected {0} {1} {2} {3}",client.id,client.displayName,client.gameVersion,client.platform.ToString());
        //Debug.Log("connected " + client.id);
        if (others.ContainsKey(client.id))
        {
            return;
        }
        string id = client.id;
        string [] parts = id.Split(',');

        Color color = Color.clear;
        color.r = float.Parse(parts[1]);
        color.g = float.Parse(parts[2]);
        color.b = float.Parse(parts[3]);

        GameObject o = Instantiate(clonePrefab);
        o.GetComponent<Clone>().SetColor(color);
        o.name = client.displayName;

        others.Add(id, o.GetComponent<Clone>());

    }

    private void OnRoomCreateFailed()
    {
       

    }

    private void OnRoomCreated()
    {
        active = true;
    }

    private void OnConnected()
    {
        
        connector.JoinRoomOrCreateIfNotExist("test");
    }

    float timeupdated;
    private void Update()
    {
        if (connector.isConnected && connector.isInActiveRoom)
        {
            if (Time.time - timeupdated > .04f)
            {
                timeupdated = Time.time;
                writer.Reset();
                
                writer.Put(player.transform.position.x);
                writer.Put(player.transform.position.y);
                writer.Put(player.transform.position.z);

                writer.Put(player.transform.rotation.x);
                writer.Put(player.transform.rotation.y);
                writer.Put(player.transform.rotation.z);
                writer.Put(player.transform.rotation.w);

                writer.Put(System.DateTime.Now.ToFileTimeUtc());
                connector.SendData(writer, DeliveryMethod.Unreliable);
            }
        }
            
    }

    // Update is called once per frame
    public void OnDisable()
    {
        connector.Disconnect();
    }
}
