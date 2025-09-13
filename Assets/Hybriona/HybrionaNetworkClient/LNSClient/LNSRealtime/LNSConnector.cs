using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using Mirror.SimpleWeb;
using UnityEngine;

public class LNSConnector : IDisposable
{



    public bool isConnected { get; private set; }
    public bool isInActiveRoom { get; private set; } = false;

    public LNSClient localClient { get; private set; }
    public LNSClient masterClient { get; private set; }
    public List<LNSClient> clients { get; private set; } = new List<LNSClient>();
    public bool isLocalPlayerMasterClient { get; private set; } = false;
    public Dictionary<string, byte[]> persistentData { get; private set; } = new Dictionary<string, byte[]>();


    public string LastConnectedRoom { get; protected set; }
    public bool runningReconnectLogic { get; private set; } = false;

    public OnConnected onConnected;
    public OnFailedToConnect onFailedToConnect;
    public OnDisconnected onDisconnected;

    public OnRoomExistsResponse onRoomExistsResponse;
    public OnRoomListReceived onRoomListReceived;
    public OnRoomCreated onRoomCreated;
    public OnRoomJoined onRoomJoined;
    public OnRoomRejoined onRoomRejoined;
    public OnDisconnectedFromRoom onDisconnectedFromRoom;
    public OnRoomCreateFailed onRoomCreateFailed;
    public OnRoomJoinFailed onRoomJoinFailed;
    public OnRoomRejoinFailed onRoomRejoinFailed;
    public OnRandomRoomJoinFailed onRandomRoomJoinFailed;

    public OnMasterClientUpdated onMasterClientUpdated;
    public OnPlayerConnected onPlayerConnected;
    public OnPlayerDisconnected onPlayerDisconnected;


    private LNSClientParameters clientParameters;
    private LNSConnectSettings settings;
    private string generatedId;
   


#if UNITY_WEBGL

    private SimpleWebClient websocketClient;
#endif

    private LNSMainThreadDispatcher threadDispatcher;
    private ILNSDataReceiver dataReceiver;
#if !UNITY_WEBGL
    private NetManager client;
    private NetPeer peer;
#endif
    private NetDataWriter clientDataWriter;
    private NetDataWriter writer;
    private LNSRoomList roomList;
    private LNSJoinRoomFilter currentRoomFilter;

    private object thelock = new object();


    private string _lastconnectedIP;
    private int _lastconnectedPort;
   
    private uint _lastConnectedRoomMasterClientUniversalId;

   
#if UNITY_WEBGL
    private TcpConfig tcpConfig;
#endif

    public LNSConnector(LNSClientParameters clientParameters, LNSConnectSettings settings, ILNSDataReceiver dataReceiver)
    {
        this.clientParameters = clientParameters;
        this.settings = settings;
        this.settings.Validate();

        this.dataReceiver = dataReceiver;
        this.threadDispatcher = LNSMainThreadDispatcher.GetInstance();

        localClient = new LNSClient();
        clientDataWriter = new NetDataWriter();
        writer = new NetDataWriter();

        



        SetClientId(this.clientParameters.id);
        SetDisplayName(this.clientParameters.displayName);
        //Write Client Data
#if UNITY_WEBGL
        clientDataWriter.Put(LNSConstants.SERVER_EVT_VERIFY_CLIENT);
#endif
        clientDataWriter.Put(this.settings.serverSecurityKey);
        clientDataWriter.Put(this.clientParameters.id);
        clientDataWriter.Put(this.clientParameters.displayName);
        clientDataWriter.Put(this.settings.gameKey);
        clientDataWriter.Put(this.settings.gameVersion);
        clientDataWriter.Put(this.settings.platform);



#if UNITY_WEBGL
        //https://stackoverflow.com/questions/10175812/how-to-generate-a-self-signed-ssl-certificate-using-openssl
        tcpConfig = new TcpConfig(true, 0, 0);

#else

        EventBasedNetListener listener = new EventBasedNetListener();
        client = new NetManager(listener);

        //List to receiveEvent
        listener.PeerConnectedEvent += Listener_PeerConnectedEvent;
        listener.PeerDisconnectedEvent += Listener_PeerDisconnectedEvent;
        listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent; ;
       
        listener.NetworkErrorEvent += Listener_NetworkErrorEvent;
        listener.NetworkReceiveUnconnectedEvent += Listener_NetworkReceiveUnconnectedEvent;
#endif
    }

    ~LNSConnector()
    {
        Dispose();
    }


#if UNITY_WEBGL
    private void WebsocketClient_onError(Exception obj)
    {
        Debug.Log("Error " + obj.Message);
       
        clients.Clear();
        isInActiveRoom = false;
        isConnected = localClient.isConnected = false;
#if UNITY_WEBGL
        try
        {
            websocketClient.Disconnect();
        }
        catch
        {

        }
#endif
        if (onDisconnected != null)
        {
            if (runningReconnectLogic)
            {
                return;
            }


            DispatchToMainThread(() =>
            {
                onDisconnected();

            });

        }
    }

    private void WebsocketClient_onDisconnect()
    {
        if(runningReconnectLogic)
        {
            return;
        }

        isInActiveRoom = false;
        isConnected = localClient.isConnected = false;
        clients.Clear();
        if (onDisconnected != null)
        {
            DispatchToMainThread(() =>
            {
                onDisconnected();

            });

        }
    }

    private void WebsocketClient_onData(ArraySegment<byte> data)
    {
        LNSReader reader = LNSReader.GetFromPool();
        reader.SetSource(data.Array,data.Offset,data.Count);
        var instruction = reader.GetByte();

        ProcessReceivedData(instruction, reader, DeliveryMethod.ReliableOrdered);

       
    }

    private void WebsocketClient_onConnect()
    {
        websocketClient.Send(new ArraySegment<byte>(clientDataWriter.Data, 0, clientDataWriter.Length));
    }

#endif


    public void SetDataReceiver(ILNSDataReceiver dataReceiver)
    {
        this.dataReceiver = dataReceiver;
    }

    public bool SetClientId(string id)
    {
        if (!isConnected)
        {
            localClient.generatedId = this.generatedId = id;
        }
        return false;
    }

    public bool SetDisplayName(string displayName)
    {
        if (isConnected && isInActiveRoom)
        {
            return false;
        }
        localClient.displayName = displayName;
        return true;
    }

    public int GetPing()
    {
        if (isConnected)
        {
#if !UNITY_WEBGL
            return peer.Ping;
#endif
        }
        return -1;
    }

    public bool Connect()
    {
        return Connect(this.settings.serverIp, this.settings.serverPort);
    }

    public bool Connect(string ip, int port)
    {
        if (isConnected)
        {
            return false;
        }
        _lastconnectedIP = ip;
        _lastconnectedPort = port;
        //new Thread(() =>{

#if UNITY_WEBGL

        runningReconnectLogic = false;
        StartWebGLSocketConnection(ip,port);
#else
        client.Start();
        StartUpdateLoop();
        peer = client.Connect(ip, port, clientDataWriter);
#endif


        //}).Start();
        return true;
    }

   

#if UNITY_WEBGL
    private Coroutine webGLLooper;

    private void StartWebGLSocketConnection(string ip,int port)
    {
        if (webGLLooper != null)
        {
            threadDispatcher.StopCoroutine(webGLLooper);
        }


        websocketClient = SimpleWebClient.Create(16 * 1024, 3000, tcpConfig);

        websocketClient.onConnect += WebsocketClient_onConnect;
        websocketClient.onData += WebsocketClient_onData;
        websocketClient.onDisconnect += WebsocketClient_onDisconnect;
        websocketClient.onError += WebsocketClient_onError;

        webGLLooper = threadDispatcher.StartCoroutine(StartUpdateLoopWebGL());

        if (ip.Contains("localhost"))
        {
            websocketClient.Connect(new Uri("ws://" + ip + ":" + (port + 1)));
        }
        else
        {
            websocketClient.Connect(new Uri("wss://" + ip + ":" + (port + 1)));

        }
    }

    IEnumerator StartUpdateLoopWebGL()
    {
        WaitForSeconds waitForSeconds = new WaitForSeconds(.04f);
        if (websocketClient != null)
        {
            while (true)
            {
                websocketClient.ProcessMessageQueue();
                yield return waitForSeconds;
            }
        }

    }

    IEnumerator WebGLReconnect(int retries)
    {
       
        for (int i=0;i<retries;i++)
        {
            Debug.Log("Reconnecting: Begin " + (i+1));

            StartWebGLSocketConnection(_lastconnectedIP,_lastconnectedPort);
            while(websocketClient.ConnectionState == ClientState.Connecting)
            {
                yield return null;
            }
            if(websocketClient.ConnectionState == ClientState.Connected)
            {
                Debug.Log("Reconnecting : Connected");
                while(!isConnected)
                {
                    yield return null;
                }
                Debug.Log("Reconnecting : Client Verified");
                localClient.isConnected = isConnected = true;
                Debug.Log("Reconnecting : Rejoining Room");
                RejoinLastRoom();
                runningReconnectLogic = false;
                yield break;
            }

            float timer = 5;
            while (timer > 0)
            {
                timer -= Time.deltaTime;
                yield return null;
            }
        }

       

        bool wasConnected = isConnected;
        localClient.isConnected = isConnected = isInActiveRoom = false;
        isConnected = localClient.isConnected = false;
        LastConnectedRoom = null;
        _lastConnectedRoomMasterClientUniversalId = 0;
        runningReconnectLogic = false;
        clients.Clear();
        if (onDisconnected != null)
        {
            DispatchToMainThread(() =>
            {
                onDisconnected();
            });

        }

    }
#else
    protected void StartUpdateLoop()
    {

        if (client.IsRunning)
        {
            new Thread(() =>
            {
                while (true)
                {
                    client.PollEvents();
                    Thread.Sleep(30);
                }

            }).Start();
        }


    }
#endif

#if !UNITY_WEBGL
    public int GetMaxSinglePacketSize(DeliveryMethod deliveryMethod)
    {
        if (isConnected)
        {

            return peer.GetMaxSinglePacketSize(deliveryMethod);
        }
        else
        {
            return 0;
        }
    }
#endif

    public bool ReconnectAndRejoin(int retries = 20, string roomid = null)
    {
        if (runningReconnectLogic)
            return false;

        if (!string.IsNullOrEmpty(roomid))
        {
            LastConnectedRoom = roomid;
        }
        if (!isConnected && !isInActiveRoom && WasConnectedToARoom())
        {
            runningReconnectLogic = true;

#if !UNITY_WEBGL
            new Thread(() =>
            {
                for (int i = 0; i < retries; i++)
                {


                    Debug.Log("Reconnecting: Begin " + (i+1));

                    client.TriggerUpdate();
                    peer = client.Connect(_lastconnectedIP, _lastconnectedPort, clientDataWriter);
                    while (peer.ConnectionState == ConnectionState.Outgoing)
                    {
                        Thread.Sleep(10);
                    }

                    if (peer.ConnectionState == ConnectionState.Connected)
                    {
                        while(!isConnected)
                        {
                            Thread.Sleep(10);
                        }
                        runningReconnectLogic = false;
                        Debug.Log("Reconnecting : Connected");
                        localClient.isConnected = isConnected = true;
                        Debug.Log("Reconnecting : Rejoining Room");
                        StartUpdateLoop();
                        RejoinLastRoom();
                        return;
                    }

                    Debug.Log("Reconnecting : Not Connected");
                    Thread.Sleep(5000);
                }

                bool wasConnected = isConnected;
                localClient.isConnected = isConnected = isInActiveRoom = false;
                isConnected = localClient.isConnected = false;
                LastConnectedRoom = null;
                _lastConnectedRoomMasterClientId = null;
                runningReconnectLogic = false;
                clients.Clear();
                if (onDisconnected != null)
                {
                    DispatchToMainThread(() =>
                    {
                        onDisconnected();

                    });

                }

            }).Start();

#else

            threadDispatcher.StartCoroutine(WebGLReconnect(retries));

#endif

            return true;
        }
        return false;
    }




    public bool WasConnectedToARoom()
    {
        return !string.IsNullOrEmpty(LastConnectedRoom);
    }

    public void Disconnect()
    {
        LastConnectedRoom = null;
        _lastconnectedIP = null;

        clients.Clear();
#if UNITY_WEBGL
        websocketClient.Disconnect();
#else
        client.DisconnectAll();
#endif
        localClient.isConnected = isConnected = isInActiveRoom = false;
    }

    public bool FetchRoomList()
    {
        return FetchRoomList(currentRoomFilter);
    }

    public bool QueryIfRoomExists(string roomdId)
    {
        if (isConnected && !isInActiveRoom)
        {
            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_ROOM_EXIST_QUERY);
                writer.Put(roomdId);
#if UNITY_WEBGL
                websocketClient.Send(new ArraySegment<byte>(writer.Data, 0, writer.Length));
#else
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
#endif
            }
            return true;
        }

        return false;
    }

    public bool FetchRoomList(LNSJoinRoomFilter filter)
    {
        if (isConnected && !isInActiveRoom)
        {
            currentRoomFilter = filter;
            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_FETCH_ROOM_LIST);
                if (filter != null)
                {
                    filter.AppendToWriter(writer);
                }
                else
                {
                    writer.Put((byte)0);
                }

#if UNITY_WEBGL
                websocketClient.Send(new ArraySegment<byte>(writer.Data, 0, writer.Length));
#else
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
#endif

            }
            return true;
        }
        return false;
    }

    public bool CreateRoom(string roomid, LNSCreateRoomParameters parameters)
    {
        if (isConnected && !isInActiveRoom)
        {
            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_CREATE_ROOM);
                writer.Put(roomid);
                parameters.AppendToWriter(writer);

#if UNITY_WEBGL
                websocketClient.Send(new ArraySegment<byte>(writer.Data, 0, writer.Length));
#else
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
#endif

            }
            return true;
        }
        return false;
    }

    public bool JoinRoom(string roomid, string password = null)
    {
        if (isConnected && !isInActiveRoom)
        {
            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_JOIN_ROOM);
                writer.Put(roomid);
                if (string.IsNullOrEmpty(password))
                {
                    writer.Put(false);
                }
                else
                {
                    writer.Put(true);
                    writer.Put(password);
                }

#if UNITY_WEBGL
                websocketClient.Send(new ArraySegment<byte>(writer.Data, 0, writer.Length));
#else
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
#endif
            }
            return true;
        }
        return false;
    }

    public bool JoinRandomRoom(LNSJoinRoomFilter filter)
    {
        if (isConnected && !isInActiveRoom)
        {

            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_JOIN_RANDOM_ROOM);
                if (filter == null)
                {
                    writer.Put((byte)0);
                }
                else
                {
                    filter.AppendToWriter(writer);
                }
#if UNITY_WEBGL
                websocketClient.Send(new ArraySegment<byte>(writer.Data, 0, writer.Length));
#else
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
#endif
            }
            return true;
        }
        return false;
    }

    public bool RejoinRoom(string roomid)
    {
        if (isConnected && !isInActiveRoom)
        {
            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_REJOIN_ROOM);
                writer.Put(roomid);
#if UNITY_WEBGL
                websocketClient.Send(new ArraySegment<byte>(writer.Data, 0, writer.Length));
#else
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
#endif
            }
            return true;
        }
        return false;
    }

    public bool RejoinLastRoom()
    {
        if (isConnected && !isInActiveRoom)
        {
            Debug.Log("Connecting "+LastConnectedRoom);
            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_REJOIN_ROOM);
                writer.Put(LastConnectedRoom);

#if UNITY_WEBGL
                websocketClient.Send(new ArraySegment<byte>(writer.Data, 0, writer.Length));
#else
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
#endif
            }
            return true;
        }
        return false;
    }

    public bool JoinRoomOrCreateIfNotExist(string roomid, int maxPlayers = 1000)
    {
        if (isConnected && !isInActiveRoom)
        {
            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_CREATE_OR_JOIN_ROOM);
                //WritePlayerData(writer);
                writer.Put(roomid);
                writer.Put(maxPlayers);
#if UNITY_WEBGL
                websocketClient.Send(new ArraySegment<byte>(writer.Data, 0, writer.Length));
#else
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
#endif
            }
            return true;
        }
        return false;
    }

    public bool LockRoom()
    {
        if (isConnected && isInActiveRoom)
        {
            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_LOCK_ROOM);
#if UNITY_WEBGL
                websocketClient.Send(new ArraySegment<byte>(writer.Data, 0, writer.Length));
#else
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
#endif
            }
            return true;
        }
        return false;
    }

    public bool UnLockRoom()
    {
        if (isConnected && isInActiveRoom)
        {
            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_UNLOCK_ROOM);
#if UNITY_WEBGL
                websocketClient.Send(new ArraySegment<byte>(writer.Data, 0, writer.Length));
#else
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
#endif
            }
            return true;
        }
        return false;
    }




    public bool RaiseEventOnAll(ushort eventCode, LNSWriter m_writer, DeliveryMethod deliveryMethod)
    {
        if (isConnected && isInActiveRoom)
        {
            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_RAW_DATA_NOCACHE);
                writer.Put(eventCode);
                writer.Put(m_writer.Data, 0, m_writer.Length);

#if UNITY_WEBGL
                websocketClient.Send(new ArraySegment<byte>(writer.Data, 0, writer.Length));
#else
                 if (deliveryMethod != DeliveryMethod.ReliableOrdered &&
                    deliveryMethod != DeliveryMethod.ReliableUnordered && peer.GetMaxSinglePacketSize(deliveryMethod) - 4 < writer.Length)
                {
                    Debug.LogError("Packet data is too large. Switching to ReliableOrdered method");
                    deliveryMethod = DeliveryMethod.ReliableOrdered;
                }
                peer.Send(writer, deliveryMethod);
#endif



            }
            return true;
        }
        return false;
    }


    public bool RaisePingToServer()
    {
        if (isConnected)
        {
            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_CLIENT_PING);
                writer.Put((byte)1);

#if UNITY_WEBGL
                websocketClient.Send(new ArraySegment<byte>(writer.Data, 0, writer.Length));
#else
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
#endif
                return true;
            }
        }
        return false;
    }
    public bool RaiseEventOnAll(ushort eventCode, byte[] rawData, DeliveryMethod deliveryMethod)
    {
        if (isConnected && isInActiveRoom)
        {
            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_RAW_DATA_NOCACHE);
                writer.Put(eventCode);
                writer.Put(rawData);

#if UNITY_WEBGL
                websocketClient.Send(new ArraySegment<byte>(writer.Data, 0, writer.Length));
#else
                if (deliveryMethod != DeliveryMethod.ReliableOrdered &&
                   deliveryMethod != DeliveryMethod.ReliableUnordered && peer.GetMaxSinglePacketSize(deliveryMethod) - 4 < writer.Length)
                {
                    Debug.LogError("Packet data is too large. Switching to ReliableOrdered method");
                    deliveryMethod = DeliveryMethod.ReliableOrdered;
                }
                peer.Send(writer, deliveryMethod);
#endif

            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// RaiseEventOnAll but with Quad tree optimizations
    /// </summary>
    /// <param name="eventCode"></param>
    /// <param name="m_writer"></param>
    /// <param name="deliveryMethod"></param>
    /// <returns></returns>
    public bool RaiseEventOnNearby(ushort eventCode, Vector2 position, float extends, LNSWriter m_writer, DeliveryMethod deliveryMethod)
    {
        if (isConnected && isInActiveRoom)
        {
            lock (thelock)
            {

                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_RAW_DATA_TO_NEARBY_CLIENTS);

                Rect searchRect = new Rect(Vector3.zero, new Vector2(extends * 2, extends * 2));
                searchRect.center = position;
                writer.Put(searchRect.x);
                writer.Put(searchRect.y);
                writer.Put(searchRect.size.x);
                writer.Put(searchRect.size.y);

                //Debug.LogFormat("From {0} - Search Rect {1},{2} {3},{4} - Position {5},{6}", "Client", searchRect.x, searchRect.x, searchRect.width, searchRect.height, position.x, position.y);
                writer.Put(eventCode);
                writer.Put(m_writer.Data, 0, m_writer.Length);

#if UNITY_WEBGL
                websocketClient.Send(new ArraySegment<byte>(writer.Data, 0, writer.Length));
#else
                if (deliveryMethod != DeliveryMethod.ReliableOrdered &&
                    deliveryMethod != DeliveryMethod.ReliableUnordered && peer.GetMaxSinglePacketSize(deliveryMethod) - 4 < writer.Length)
                {
                    Debug.LogError("Packet data is too large. Switching to ReliableOrdered method");
                    deliveryMethod = DeliveryMethod.ReliableOrdered;
                }
                peer.Send(writer, deliveryMethod);
#endif

            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// RaiseEventOnAll but with Quad tree optimizations
    /// </summary>
    /// <param name="eventCode"></param>
    /// <param name="position of rect"></param>
    /// <param name="extends of rect"></param>
    /// <param name="rawData"></param>
    /// <param name="deliveryMethod"></param>
    /// <returns></returns>
    public bool RaiseEventOnNearby(ushort eventCode, Vector2 position, float extends, byte[] rawData, DeliveryMethod deliveryMethod)
    {
        if (isConnected && isInActiveRoom)
        {
            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_RAW_DATA_TO_NEARBY_CLIENTS);

                Rect searchRect = new Rect(Vector3.zero, new Vector2(extends * 2, extends * 2));
                searchRect.center = position;
                writer.Put(searchRect.x);
                writer.Put(searchRect.y);
                writer.Put(searchRect.size.x);
                writer.Put(searchRect.size.y);

                //Debug.LogFormat("From {0} - Search Rect {1},{2} {3},{4} - Position {5},{6}", "Client", searchRect.x, searchRect.x, searchRect.width, searchRect.height, position.x, position.y);
                writer.Put(eventCode);
                writer.Put(rawData);

#if UNITY_WEBGL
                websocketClient.Send(new ArraySegment<byte>(writer.Data, 0, writer.Length));
#else
                if (deliveryMethod != DeliveryMethod.ReliableOrdered &&
                   deliveryMethod != DeliveryMethod.ReliableUnordered && peer.GetMaxSinglePacketSize(deliveryMethod) - 4 < writer.Length)
                {
                    Debug.LogError("Packet data is too large. Switching to ReliableOrdered method");
                    deliveryMethod = DeliveryMethod.ReliableOrdered;
                }
                peer.Send(writer, deliveryMethod);
#endif
            }

            return true;
        }
        return false;
    }



    public bool RaiseEventOnClient(LNSClient client, ushort eventCode, LNSWriter m_writer, DeliveryMethod deliveryMethod)
    {
        return RaiseEventOnClient(client.universalId, eventCode, m_writer, deliveryMethod);
    }

    public bool RaiseEventOnClient(LNSClient client, ushort eventCode, byte[] rawData, DeliveryMethod deliveryMethod)
    {
        return RaiseEventOnClient(client.universalId, eventCode, rawData, deliveryMethod);
    }

    public bool RaiseEventOnMasterClient(ushort eventCode, LNSWriter m_writer, DeliveryMethod deliveryMethod)
    {
        if (isLocalPlayerMasterClient)
        {
            return false;
        }


        if (masterClient != null)
        {
            return RaiseEventOnClient(masterClient.universalId, eventCode, m_writer, deliveryMethod);
        }
        return false;
    }

    public bool RaiseEventOnMasterClient(ushort eventCode, byte[] rawData, DeliveryMethod deliveryMethod)
    {
        if (isLocalPlayerMasterClient)
        {
            return false;
        }


        if (masterClient != null)
        {
            return RaiseEventOnClient(masterClient.universalId, eventCode, rawData, deliveryMethod);
        }
        return false;
    }

    public bool RaiseEventOnClient(uint universalId, ushort eventCode, LNSWriter m_writer, DeliveryMethod deliveryMethod)
    {
        if (isConnected && isInActiveRoom)
        {

            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_RAW_DATA_TO_CLIENT);
                writer.Put(universalId);
                writer.Put(eventCode);
                writer.Put(m_writer.Data, 0, m_writer.Length);

#if UNITY_WEBGL
                websocketClient.Send(new ArraySegment<byte>(writer.Data, 0, writer.Length));
#else
                if (deliveryMethod != DeliveryMethod.ReliableOrdered &&
                   deliveryMethod != DeliveryMethod.ReliableUnordered && peer.GetMaxSinglePacketSize(deliveryMethod) - 4 < writer.Length)
                {
                    Debug.LogError("Packet data is too large. Switching to ReliableOrdered method");
                    deliveryMethod = DeliveryMethod.ReliableOrdered;
                }

                peer.Send(writer, deliveryMethod);
#endif

            }

            return true;
        }
        return false;
    }

    public bool RaiseEventOnClient(uint universalId, ushort eventCode, byte[] rawData, DeliveryMethod deliveryMethod)
    {
        if (isConnected && isInActiveRoom)
        {

            //new Thread(() =>{
            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_RAW_DATA_TO_CLIENT);
                writer.Put(universalId);
                writer.Put(eventCode);
                writer.Put(rawData);
#if UNITY_WEBGL
                websocketClient.Send(new ArraySegment<byte>(writer.Data, 0, writer.Length));
#else
                if (deliveryMethod != DeliveryMethod.ReliableOrdered &&
                   deliveryMethod != DeliveryMethod.ReliableUnordered && peer.GetMaxSinglePacketSize(deliveryMethod) - 4 < writer.Length)
                {
                    Debug.LogError("Packet data is too large. Switching to ReliableOrdered method");
                    deliveryMethod = DeliveryMethod.ReliableOrdered;
                }
                peer.Send(writer, deliveryMethod);
#endif
                //peer.Send(writer, deliveryMethod);
            }
            //}).Start();
            return true;
        }
        return false;
    }

    public bool SendCachedDataToAll(string key, LNSWriter m_writer)
    {
        if (isConnected && isInActiveRoom)
        {
            if (isLocalPlayerMasterClient)
            {
                //new Thread(() =>{
                lock (thelock)
                {
                    writer.Reset();
                    writer.Put(LNSConstants.SERVER_EVT_RAW_DATA_CACHE);
                    writer.Put(key);
                    writer.Put(localClient.universalId);
                    writer.Put(m_writer.Data, 0, m_writer.Length);
#if UNITY_WEBGL
                    websocketClient.Send(new ArraySegment<byte>(writer.Data, 0, writer.Length));
#else
                        peer.Send(writer, DeliveryMethod.ReliableOrdered);
#endif

                }
                //}).Start();
                return true;
            }
            else
            {
                throw new Exception("Only master client can send Cached Data to server");
            }


        }
        return false;

    }

    public bool SendCachedDataToAll(string key, byte[] rawData)
    {
        if (isConnected && isInActiveRoom)
        {
            if (isLocalPlayerMasterClient)
            {

                lock (thelock)
                {
                    writer.Reset();
                    writer.Put(LNSConstants.SERVER_EVT_RAW_DATA_CACHE);
                    writer.Put(key);
                    writer.Put(localClient.universalId);
                    writer.Put(rawData);
#if UNITY_WEBGL
                    websocketClient.Send(new ArraySegment<byte>(writer.Data, 0, writer.Length));
#else
                    peer.Send(writer, DeliveryMethod.ReliableOrdered);
#endif

                }

                return true;
            }
            else
            {
                throw new Exception("Only master client can send Cached Data to server");
            }


        }
        return false;

    }

    public bool MakeMeMasterClient()
    {
        if (isConnected && isInActiveRoom)
        {
            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_MAKE_ME_MASTERCLIENT);

#if UNITY_WEBGL
                websocketClient.Send(new ArraySegment<byte>(writer.Data, 0, writer.Length));
#else
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
#endif

            }
            return true;
        }
        return false;
    }

    public void DisconnectFromRoom()
    {
        if (isConnected && isInActiveRoom)
        {
           
            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_LEAVE_ROOM);
#if UNITY_WEBGL
                websocketClient.Send(new ArraySegment<byte>(writer.Data, 0, writer.Length));
#else
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
#endif
            }

        }
    }


    //protected void WritePlayerData(NetDataWriter writer)
    //{
    //    writer.Put(id);
    //    writer.Put(displayName);
    //    writer.Put(this.settings.gameKey);
    //    writer.Put(this.settings.gameVersion);
    //    writer.Put(this.settings.platform);
    //}


    private void ProcessReceivedData(byte clientInstruction, LNSReader reader, DeliveryMethod deliveryMethod)
    {

       

        if(clientInstruction == LNSConstants.CLIENT_EVT_VERIFIED)
        {
            localClient.universalId = reader.GetUInt();
           
            localClient.isConnected = isConnected = true;

            if (!runningReconnectLogic)
            {
                if (onConnected != null)
                {
                    DispatchToMainThread(() =>
                    {
                        onConnected();
                    });
                }
            }

        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_LIST)
        {
            if (roomList == null)
            {
                roomList = new LNSRoomList();
            }
            JsonUtility.FromJsonOverwrite(reader.GetString(), roomList);
            if (onRoomListReceived != null)
            {
                threadDispatcher.Add(() => onRoomListReceived(roomList));
            }
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_CREATED)
        {
            isInActiveRoom = true;
            LastConnectedRoom = reader.GetString();


            if (onRoomCreated != null)
            {
                threadDispatcher.Add(() => onRoomCreated());
            }
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_DISCONNECTED)
        {
            clients.Clear();
            isLocalPlayerMasterClient = false;
            localClient.isMasterClient = false;
            LastConnectedRoom = null;
            isInActiveRoom = false;

            if (onDisconnectedFromRoom != null)
            {
                threadDispatcher.Add(() => onDisconnectedFromRoom());
            }
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_FAILED_CREATE)
        {
            byte reason = reader.GetByte();
            if (onRoomCreateFailed != null)
            {
                threadDispatcher.Add(() => onRoomCreateFailed((ROOM_FAILURE_CODE)reason));
            }
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_FAILED_JOIN)
        {
            byte reason = reader.GetByte();
            if (onRoomJoinFailed != null)
            {
                threadDispatcher.Add(() => onRoomJoinFailed((ROOM_FAILURE_CODE)reason));
            }
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_FAILED_REJOIN)
        {
            byte reason = reader.GetByte();
            if (onRoomRejoinFailed != null)
            {
                threadDispatcher.Add(() => onRoomRejoinFailed((ROOM_FAILURE_CODE)reason));
            }
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_JOINED)
        {
            isInActiveRoom = true;
            LastConnectedRoom = reader.GetString();

            if (onRoomJoined != null)
            {
                threadDispatcher.Add(() => onRoomJoined());
            }
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_REJOINED)
        {
            isInActiveRoom = true;
            LastConnectedRoom = reader.GetString();
            if (onRoomRejoined != null)
            {
                threadDispatcher.Add(() => onRoomRejoined());
            }

          
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_FAILED_RANDOM_JOIN)
        {
            isInActiveRoom = false;
            if (onRandomRoomJoinFailed != null)
            {
                threadDispatcher.Add(() => onRandomRoomJoinFailed());
            }
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_EXISTS_RESPONSE)
        {
            string roomId = reader.GetString();
            bool roomExists = reader.GetBool();

            if (onRoomExistsResponse != null)
            {
                threadDispatcher.Add(() => onRoomExistsResponse(roomId, roomExists));
            }
            //_lastConnectedRoom = packetReader.GetString();
            //if (onRoomRejoined != null)
            //{
            //    threadDispatcher.Add(() => onRoomRejoined());
            //}
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_MASTERCLIENT_CHANGED)
        {
            _lastConnectedRoomMasterClientUniversalId = reader.GetUInt();
            localClient.isMasterClient = isLocalPlayerMasterClient = (_lastConnectedRoomMasterClientUniversalId == localClient.universalId);

            masterClient = clients.Find(client => client.universalId == _lastConnectedRoomMasterClientUniversalId);
            if (onMasterClientUpdated != null)
            {
                try
                {
                    if (isLocalPlayerMasterClient)
                    {
                        threadDispatcher.Add(() =>
                        {
                            onMasterClientUpdated(localClient);
                        });
                    }
                    else
                    {


                        for (int i = 0; i < clients.Count; i++)
                        {
                            if (clients[i].universalId == _lastConnectedRoomMasterClientUniversalId)
                            {
                                clients[i].isMasterClient = true;
                                LNSClient client = clients[i];
                                threadDispatcher.Add(() =>
                                {

                                    onMasterClientUpdated(client);
                                });

                            }
                            else
                            {
                                clients[i].isMasterClient = false;
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError(ex.Message + " " + ex.StackTrace);
                }

            }

        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_PLAYER_CONNECTED)
        {
            
            string client_gen_id = reader.GetString();
            string client_displayName = reader.GetString();
            CLIENT_PLATFORM client_platform = (CLIENT_PLATFORM)reader.GetByte();
           
            uint universalId = reader.GetUInt();
            //Debug.Log($"connected: {client_id} with uid: {universalId}");
            LNSClient currentClient = clients.Find(client => client.universalId == universalId);

            if (currentClient == null)
            {                     
                currentClient = new LNSClient();
                currentClient.generatedId = client_gen_id;
               
                clients.Add(currentClient);
            }

            currentClient.displayName = client_displayName;
            currentClient.platform = client_platform;
            
            currentClient.universalId = universalId;

            currentClient.isConnected = true;

            if (onPlayerConnected != null)
            {
                threadDispatcher.Add(() =>
                {
                    onPlayerConnected(currentClient);
                });
            }
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_PLAYER_DISCONNECTED)
        {
            var client_uid = reader.GetUInt();
            var currentClient = clients.Find(client => client.universalId == client_uid);
            if (currentClient != null)
            {
                currentClient.isConnected = false;
                clients.Remove(currentClient);
                if (onPlayerDisconnected != null)
                {
                    threadDispatcher.Add(() =>
                    {
                        onPlayerDisconnected(currentClient);
                    });
                }
            }
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_CACHE_DATA)
        {
            string key = reader.GetString();
            var fromClientId = reader.GetUInt();
            byte[] data = reader.GetRemainingBytes();


            if (persistentData.ContainsKey(key))
            {
                persistentData[key] = data;
            }
            else
            {
                persistentData.Add(key, data);
            }
            if (fromClientId != localClient.universalId)
            {
                if (dataReceiver != null)
                {
                    threadDispatcher.Add(() =>
                    {
                        try
                        {
                            dataReceiver.OnCachedDataReceived(key, data);
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError(ex.Message + " " + ex.StackTrace);
                        }



                    });
                }
            }


        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_RAW)
        {
            var fromId = reader.GetUInt();

            var currentClient = clients.Find(client => client.universalId == fromId);
            if (currentClient != null)
            {
                if (dataReceiver != null)
                {
                    ushort eventCode = reader.GetUShort();
                    LNSReader subReader = LNSReader.GetFromPool();

                    LNSClient fromClient = currentClient;
                    DeliveryMethod _deliveryMethod = deliveryMethod;



                    //subReader.SetSource(reader.RawData,reader.Position, reader.AvailableBytes);
                    subReader.SetSource(reader.GetRemainingBytes());

                    threadDispatcher.Add(() =>
                    {
                        try
                        {
                            dataReceiver.OnEventRaised(fromClient, eventCode, subReader, _deliveryMethod);
                        }
                        catch (System.Exception ex)
                        {
                            //Debug.Log($"reader.Position: {reader.Position} reader.AvailableBytes: {reader.AvailableBytes}");
                            Debug.LogError(ex.Message + " " + ex.StackTrace);
                        }
                        subReader.Recycle();


                    });
                    //return;


                }
            }



        }
        reader.Recycle();
    }
    private void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader packetReader, byte channel, DeliveryMethod deliveryMethod)
    {
        LNSReader reader = LNSReader.GetFromPool();
        reader.SetSource(packetReader.RawData, packetReader.Position, packetReader.RawDataSize);
        ProcessReceivedData(reader.GetByte(), reader, deliveryMethod);
        packetReader.Recycle();
    }

    private void Listener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        bool wasConnected = isConnected;
        localClient.isConnected = isConnected = isInActiveRoom = false;
        if (wasConnected)
        {
            clients.Clear();
            if (onDisconnected != null)
            {
                DispatchToMainThread(() =>
                {
                    onDisconnected();

                });

            }
        }

        else if (onFailedToConnect != null)
        {
            try
            {
                //Debug.LogError(disconnectInfo.Reason);
                if (disconnectInfo.Reason == DisconnectReason.ConnectionRejected)
                {
                    byte ins = disconnectInfo.AdditionalData.GetByte();
                    if (ins == LNSConstants.CLIENT_EVT_SERVER_EXECEPTION)
                    {

                        DispatchToMainThread(() =>
                        {
                            onFailedToConnect(CONNECTION_FAILURE_CODE.SERVER_EXECPTION);
                        });
                    }
                    if (ins == LNSConstants.CLIENT_EVT_UNAUTHORIZED_CONNECTION)
                    {

                        DispatchToMainThread(() =>
                        {
                            onFailedToConnect(CONNECTION_FAILURE_CODE.CONNECTION_IS_NOT_AUTHORIZED);
                        });
                    }
                    else if (ins == LNSConstants.CLIENT_EVT_UNAUTHORIZED_GAME)
                    {

                        DispatchToMainThread(() =>
                        {
                            onFailedToConnect(CONNECTION_FAILURE_CODE.GAME_IS_NOT_AUTHORIZED);
                        });
                    }
                    else if (ins == LNSConstants.CLIENT_EVT_USER_ALREADY_CONNECTED)
                    {

                        DispatchToMainThread(() =>
                        {
                            onFailedToConnect(CONNECTION_FAILURE_CODE.USER_IS_ALREADY_CONNECTED);
                        });
                    }
                    else
                    {
                        DispatchToMainThread(() =>
                        {
                            onFailedToConnect(CONNECTION_FAILURE_CODE.UNKNOWN_ERROR);
                        });
                    }
                }
                else
                {
                    DispatchToMainThread(() =>
                    {
                        onFailedToConnect(CONNECTION_FAILURE_CODE.COULD_NOT_CONNECT_TO_HOST);
                    });
                }


            }
            catch (System.Exception ex)
            {

                DispatchToMainThread(() =>
                {
                    onFailedToConnect();
                });
            }
        }

    }

    private void Listener_PeerConnectedEvent(NetPeer peer)
    {
        /*
         
        localClient.isConnected = isConnected = true;
        if (onConnected != null)
        {
            DispatchToMainThread(() =>
            {
                onConnected();
            });
        }

        */
    }

    private void Listener_NetworkReceiveUnconnectedEvent(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        Debug.Log("unconnected event");
    }

    private void Listener_NetworkErrorEvent(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
    {
        Debug.Log("Listener_NetworkErrorEvent");
    }


    public void DispatchToMainThread(System.Action action)
    {
        threadDispatcher.Add(() =>
        {
            if (action != null)
            {
                action();
            }

        });
    }

    public void Dispose()
    {

#if UNITY_WEBGL
        websocketClient.Disconnect();

#else
        if(client != null)
        {
            client.DisconnectAll();
        }
#endif
    }
}
