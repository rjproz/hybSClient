using System.Collections;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

public class MultiplayerClient : MonoBehaviour,ILNSDataReceiver
{
    public Transform player;
    public GameObject clonePrefab;
    public int ping;
    private Dictionary<string, Clone> others = new Dictionary<string, Clone>();

    public LNSConnector connector;
    private string id;
    private LNSWriter writer;

    private Color color;

    public void Initialize(string id)
    {
        player.position = new Vector3(Random.Range(-50, 50), .5f, Random.Range(-50, 50));

        color.r = Random.value;
        color.g = Random.value;
        color.b = Random.value;

        this.id = id;

        player.GetComponent<Renderer>().material.color = color;

        LNSConnectSettings settings = new LNSConnectSettings();
        settings.gameKey = "com.hybriona.multiplayertest";
        settings.gameVersion = Application.version;
        settings.serverIp = "45.55.33.88";
        settings.serverPort = 10002;
        settings.serverSecurityKey = "iamatestserver";

        //settings.serverIp = "192.168.0.100";
        connector = new LNSConnector(settings,this);
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

    public void OnEventRaised(LNSClient from,ushort eventCode, LNSReader reader, DeliveryMethod deliveryMethod)
    {
        if(eventCode == 0)
        {
            Color color = reader.GetColor();      
            GameObject o = Instantiate(clonePrefab);
            o.GetComponent<Clone>().SetColor(color);
            o.name = from.id + "_" + from.displayName;
            others.Add(from.id, o.GetComponent<Clone>());
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
        else if(eventCode == 2)
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
        writer.Put(color);
        connector.RaiseEventOnClient(client, 0, writer, DeliveryMethod.ReliableOrdered);
    }

    float timeupdated;
    private void Update()
    {
        if (connector.isConnected && connector.isInActiveRoom)
        {
            if (Time.time - timeupdated > .1f)
            {
                timeupdated = Time.time;
                writer.Reset();
                writer.Put(player.transform.position);
                writer.Put(player.transform.rotation);        
                //writer.Put(System.DateTime.Now.ToFileTimeUtc());
                connector.RaiseEventOnAll(1,writer, DeliveryMethod.Unreliable);
            }

            if(Input.GetKeyUp(KeyCode.Space))
            {
                writer.Reset();
                connector.RaiseEventOnMasterClient(2, writer, DeliveryMethod.ReliableUnordered);
            }
        }
        ping = connector.GetPing();
    }

    // Update is called once per frame
    public void OnDisable()
    {
        connector.Disconnect();
    }
}
