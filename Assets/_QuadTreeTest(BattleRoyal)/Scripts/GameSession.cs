using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
public class GameSession : MonoBehaviour,ILNSDataReceiver
{
    public bool useLive = true;
    public string liveip = "vps.hybriona.com";
    public string ip = "localhost";
    public int port = 10002;
    public Transform player;
    public float searchExtends = 10;
    public GameObject clonePrefab;

    public float dataTransferPerSec;

    public float mapSize { get; set; }
    private Vector3 playerTarget;

    private LNSConnector connector;
    private LNSWriter writer;

    private Dictionary<string, Transform> remoteClones = new Dictionary<string, Transform>();
    private int remoteCloneCount = 0;

    private int totalDataTransfered = 0;
    private float timeDataCountStarted = 0;
    public void StartProcess(string id)
    {
        clonePrefab.SetActive(false);
        writer = new LNSWriter();

        player.GetComponent<SphereGizmos>().radius = searchExtends;

        if(name == "Client")
        {
            id = "Client";
            player.localPosition = Vector3.zero;
        }
       

        StartCoroutine(RandomMovement());
        player.localPosition = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)) * mapSize * .5f;

        LNSClientParameters clientParameters = new LNSClientParameters(id, gameObject.name);
        LNSConnectSettings connectSettings = new LNSConnectSettings();
        if (useLive)
        {
            connectSettings.serverIp = liveip;
        }
        else
        {
            connectSettings.serverIp = ip;
        }
        connectSettings.serverPort = port;
        connectSettings.serverSecurityKey = "demokey";
        connectSettings.gameKey = "hybriona.quadtreetest";
        if (!useLive)
        {
            connectSettings.serverSecurityKey = "demokey";
        }

        connector = new LNSConnector(clientParameters, connectSettings, this);




       
        connector.onConnected = () =>
        {
            LNSCreateRoomParameters roomParameters = new LNSCreateRoomParameters();
            roomParameters.maxPlayers = 1000;
            roomParameters.isQuadTreeAllowed = true;
            //roomParameters.idleLife = 60 * 24;
            roomParameters.EnableQuadTreeCellOptimization(Vector2.zero,new Vector2(2000,2000));
            connector.CreateRoom("default", roomParameters);
        };

        connector.onFailedToConnect = (CONNECTION_FAILURE_CODE code) =>
        {
            Debug.LogError(name +" - "+ code);
        };

        connector.onDisconnectedFromRoom = () =>
        {
            Debug.LogError(name + " - onDisconnectedFromRoom");
        };

        connector.onDisconnected = () =>
        {
            Debug.LogError(name + " - onDisconnectedFromServer");
        };

        connector.onRoomCreateFailed = (ROOM_FAILURE_CODE code) =>
        {
            Debug.LogError(name + " - " + code);
            if (code == ROOM_FAILURE_CODE.ROOM_ALREADY_EXIST)
            {
                connector.JoinRoom("default");
            }
        };

        connector.onRoomJoined = () =>
        {
            Debug.Log(name + " on room joined");
        };
        connector.onRoomJoinFailed = (ROOM_FAILURE_CODE code) =>
        {
            Debug.LogError(name + " - " + code);
        };
        connector.Connect();
    }


    IEnumerator RandomMovement()
    {
        while(true)
        {
            playerTarget = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)) * mapSize * .5f;
            
            
            yield return new WaitForSeconds(Random.Range(2f, 5f));

            if(connector.isLocalPlayerMasterClient)
            {
                //writer.Reset();
                //writer.Put("nanhi hai suhani @"+Time.time);
                //connector.SendCachedDataToAll("key3", writer);
            }
        }
    }

    public void OnEventRaised(LNSClient from, ushort eventCode, LNSReader reader, DeliveryMethod deliveryMethod)
    {
        totalDataTransfered += reader.RawDataSize;
        if (Time.time - timeDataCountStarted > 1)
        {
            dataTransferPerSec = (float)totalDataTransfered / (Time.time - timeDataCountStarted);
            timeDataCountStarted = Time.time;
            totalDataTransfered = 0;
        }
        if(eventCode == 0)
        {

            if(!remoteClones.ContainsKey(from.id))
            {
                GameObject clone = GameObject.Instantiate(clonePrefab,clonePrefab.transform.parent,true);
                clone.SetActive(true);
                clone.name = "Clone_" + from.id;
                remoteClones.Add(from.id, clone.transform);
            }
            remoteClones[from.id].localPosition = reader.GetVector3();
        }

    }


    private float lastTimeDataSent;
    private void Update()
    {
        if (connector != null && connector.isConnected && connector.isInActiveRoom)
        {
            if (Time.time - lastTimeDataSent > 0.05f)
            {
                lastTimeDataSent = Time.time;
                Vector3 playerPos = player.localPosition;
                writer.Reset();
                writer.Put(playerPos);
                connector.RaiseEventOnNearby(0, new Vector2(playerPos.x, playerPos.z), searchExtends, writer, DeliveryMethod.Sequenced);
            }
            //connector.RaiseEventOnAll(0, writer, DeliveryMethod.Sequenced);
        }
        if (Vector3.Distance(player.localPosition, playerTarget) > 0.5f)
        {
            player.localPosition = Vector3.MoveTowards(player.localPosition, playerTarget, Time.deltaTime * 15f);
        }

        foreach (var element in remoteClones)
        {
            if(Vector3.Distance(player.localPosition, element.Value.transform.localPosition) > searchExtends)
            {
                element.Value.transform.localPosition = new Vector3(0,-50,0);
            }
        }
        
    }

    private void OnDisable()
    {
        if (connector != null && connector.isConnected)
        {
            connector.Disconnect();
        }
    }

    public void OnCachedDataReceived(string key, LNSReader data)
    {
        Debug.Log("Cached: "+key + "," + data.GetString());
    }
}
