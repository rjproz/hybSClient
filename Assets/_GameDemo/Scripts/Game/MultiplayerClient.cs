using System.Collections;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

public class MultiplayerClient : MonoBehaviour,ILNSDataReceiver
{
    public MainPlayer player;
    public GameObject clonePrefab;
    public bool connectToLocalServer = false;
    public int ping;
    private Dictionary<string, Clone> others = new Dictionary<string, Clone>();

    public LNSConnector connector;
    private string id;
    private LNSWriter writer;


    public void Initialize(string id)
    {


        this.id = id;

        LNSClientParameters clientParameters = new LNSClientParameters(this.id,null);


        LNSConnectSettings settings = new LNSConnectSettings();
        settings.gameKey = "com.hybriona.multiplayertest";
        settings.gameVersion = Application.version;
        if (connectToLocalServer)
        {
            settings.serverIp = "127.0.0.1";
        }
        else
        {
            settings.serverIp = "45.55.33.88";
        }
        settings.serverPort = 10002;
        settings.serverSecurityKey = "iamatestserver";

        //settings.serverIp = "192.168.0.100";
        connector = new LNSConnector(clientParameters,settings, this);
        connector.SetClientId(this.id);
        if (writer == null)
        {
            writer = new LNSWriter();
        }
        writer.Reset();

        connector.onPlayerConnected = OnPlayerConnected;
        connector.onPlayerDisconnected = OnPlayerDisconnected;
        connector.onMasterClientUpdated += (LNSClient client) => {
            Debug.Log("Masterclient changed to " + client.id);

        };
       
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

    public void SendTransform(Vector3 pos,Quaternion rot)
    {
        if (connector.isConnected && connector.isInActiveRoom)
        {
            if (Time.time - timeupdated > .1f)
            {
                timeupdated = Time.time;
                writer.Reset();
                writer.Put(pos);
                writer.Put(rot);
                //writer.Put(System.DateTime.Now.ToFileTimeUtc());
                connector.RaiseEventOnAll(1, writer, DeliveryMethod.Unreliable);
            }

           
        }
    }

    public void SendBulletInvoke(Vector3 pos,Quaternion rot)
    {
        if (connector.isConnected && connector.isInActiveRoom)
        {
            writer.Reset();
            writer.Put(pos);
            writer.Put(rot);
            connector.RaiseEventOnAll(2, writer, DeliveryMethod.Unreliable);
           
        }
    }

    public void OnEventRaised(LNSClient from,ushort eventCode, LNSReader reader, DeliveryMethod deliveryMethod)
    {
        if (eventCode == 0)
        {
            Color color = reader.GetColor();
            if (others.ContainsKey(from.id))
            {
                Clone o = others[from.id];
                o.GetComponent<Clone>().SetColor(color);
                o.gameObject.name = from.id + "_" + from.displayName;
            }
            else
            {

                GameObject o = Instantiate(clonePrefab);
                o.GetComponent<Clone>().SetColor(color);
                o.name = from.id + "_" + from.displayName;
                others.Add(from.id, o.GetComponent<Clone>());
            }
        }
        else if (eventCode == 1)
        {
            string playerid = from.id;
            Vector3 pos = reader.GetVector3();
            Quaternion rot = reader.GetQuaternion();
            //long timestamp = reader.GetLong();
            //Debug.Log(timestamp);
            //float delay = (float)(System.DateTime.UtcNow - System.DateTime.FromFileTimeUtc(timestamp)).TotalMilliseconds;
            //if (delay > 400)
            //{
            //    //Discard old packet
            //    return;
            //}
            if (others.ContainsKey(playerid))
            {
                others[playerid].SetTarget(pos, rot);
            }
        }
        else if (eventCode == 2)
        {
            if (others.ContainsKey(from.id))
            {
                Vector3 pos = reader.GetVector3();
                Quaternion rot = reader.GetQuaternion();
                others[from.id].ShootBulletAt(pos,rot);
            }
        }
        else if (eventCode == 3)
        {
            player.transform.position += Vector3.up;
        }
    }   

    private void OnPlayerDisconnected(LNSClient client)
    {
        Debug.Log("OnPlayerDisconnected " + client.id);

     
    }

    private void OnPlayerConnected(LNSClient client)
    {
        Debug.LogFormat("OnPlayerConnected {0} {1} {2}",client.id,client.displayName,client.platform.ToString());

        writer.Reset();
        writer.Put(player.color);
        connector.RaiseEventOnClient(client, 0, writer, DeliveryMethod.ReliableOrdered);
    }

    float timeupdated;
    private void Update()
    {
       
        ping = connector.GetPing();
    }

    // Update is called once per frame
    public void OnDisable()
    {
        connector.Disconnect();
    }
}
