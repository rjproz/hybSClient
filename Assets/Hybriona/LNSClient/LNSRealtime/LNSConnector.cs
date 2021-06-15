using System;
using System.Collections.Generic;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

public class LNSConnector : IDisposable
{
   

   
    public bool isConnected { get; private set; }
    public bool isInActiveRoom { get; private set; } = false;

    public LNSClient localClient { get; private set; }
    public List<LNSClient> clients { get; private set; } = new List<LNSClient>();
    public bool isLocalPlayerMasterClient { get; private set; } = false;
    public Dictionary<string, byte[]> persistentData { get; private set; } = new Dictionary<string, byte[]>();

    public OnConnected onConnected;
    public OnFailedToConnect onFailedToConnect;
    public OnDisconnected onDisconnected;

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
    private string id;
    private string displayName;

    private LNSMainThreadDispatcher threadDispatcher;
    private ILNSDataReceiver dataReceiver;
    private NetManager client;
    private NetPeer peer;
    private NetDataWriter clientDataWriter;
    private NetDataWriter writer;
    private LNSRoomList roomList;
    private LNSJoinRoomFilter currentRoomFilter;

    private object thelock = new object();


    private string _lastconnectedIP;
    private int _lastconnectedPort;
    public string _lastConnectedRoom { get; protected set; }
    private string _lastConnectedRoomMasterClient;
    public LNSConnector(LNSClientParameters clientParameters,LNSConnectSettings settings, ILNSDataReceiver dataReceiver)
    {
        this.clientParameters = clientParameters;
       
        this.settings = settings;
       

        this.dataReceiver = dataReceiver;
        this.threadDispatcher = LNSMainThreadDispatcher.GetInstance();

        localClient = new LNSClient();
        clientDataWriter = new NetDataWriter();
        writer = new NetDataWriter();

        this.settings.Validate();

        EventBasedNetListener listener = new EventBasedNetListener();
       


        client = new NetManager(listener);


        SetClientId(this.clientParameters.id);

        //Write Client Data
        clientDataWriter.Put(this.settings.serverSecurityKey);
        clientDataWriter.Put(this.clientParameters.id);
        clientDataWriter.Put(this.clientParameters.displayName);
        clientDataWriter.Put(this.settings.gameKey);
        clientDataWriter.Put(this.settings.gameVersion);
        clientDataWriter.Put(this.settings.platform);


        //List to receiveEvent
        listener.PeerConnectedEvent += Listener_PeerConnectedEvent;
        listener.PeerDisconnectedEvent += Listener_PeerDisconnectedEvent;
        listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;
       
        listener.NetworkErrorEvent += Listener_NetworkErrorEvent;
        listener.NetworkReceiveUnconnectedEvent += Listener_NetworkReceiveUnconnectedEvent;

    }

    ~LNSConnector()
    {
        Dispose();
    }

    
    public bool SetClientId(string id)
    {
        if (!isConnected)
        {   
            localClient.id = this.id = id;
        }
        return false;
    }

    public bool SetDisplayName(string displayName)
    {
        if(isConnected && isInActiveRoom)
        {
            return false;
        }
        localClient.displayName = this.displayName = displayName;
        return true;
    }

    public int GetPing()
    {
        if(isConnected)
        {
            return peer.Ping;
        }
        return -1;
    }

    public bool Connect()
    {
        return Connect(this.settings.serverIp, this.settings.serverPort);
    }

    public bool Connect(string ip, int port)
    {
        if(isConnected)
        {
            return false;
        }
        _lastconnectedIP = ip;
        _lastconnectedPort = port;
        new Thread(() =>
        {
          
            client.Start();
            Update();
            peer = client.Connect(ip, port, clientDataWriter);


        }).Start();
        return true;
    }

    protected void Update()
    {
        if(client.IsRunning)
        new Thread(() =>
        {
            while (true)
            {
                client.PollEvents();
                Thread.Sleep(15);
            }
            /*
            while (isConnected)
            {
                
                if (peer.ConnectionState == ConnectionState.Disconnected)
                {
                    threadDispatcher.Add(() =>
                    {
                        localClient.isConnected = isConnected = false;
                        isInActiveRoom = false;
                        if (onDisconnected != null)
                        {
                            onDisconnected();
                        }
                    });
                    if (client != null)
                    {
                        client.Stop();
                    }
                    return;
                }
                client.PollEvents();
                Thread.Sleep(15);
            }
            client.Stop();
            */
        }).Start();

    }

    //public LNSClient FindRemoteClient(string clientid)
    //{
    //    for(int i=0;i<client.co)
    //}
    //public LNSClient FindRemoteClient(int clientNetId)
    //{

    //}

    public bool ReconnectAndRejoin(int retries = 20)
    {
       
        if (!isConnected && !isInActiveRoom && WasConnectedToARoom())
        {
            //TODO Reconnect logic
            new Thread(() =>
            {
                for (int i = 0; i < retries; i++)
                {
                  

                    Debug.Log("Reconnecting: Begin "+i);
                    client.TriggerUpdate();
                    peer = client.Connect(_lastconnectedIP, _lastconnectedPort, clientDataWriter);
                    while (peer.ConnectionState == ConnectionState.Outgoing)
                    {
                        Thread.Sleep(10);
                    }

                    if (peer.ConnectionState == ConnectionState.Connected)
                    {
                        Debug.Log("Reconnecting : Connected");
                        localClient.isConnected = isConnected = true;
                        Debug.Log("Reconnecting : Rejoining Room");
                        Update();
                        RejoinLastRoom();
                        return;
                    }
                    Debug.Log("Reconnecting : Not Connected");
                    Thread.Sleep(5000);
                }

                bool wasConnected = isConnected;
                localClient.isConnected = isConnected = isInActiveRoom = false;
                isConnected = localClient.isConnected = false;
                _lastConnectedRoom = null;
                _lastConnectedRoomMasterClient = null;

                if (onDisconnected != null)
                {
                    DispatchToMainThread(() =>
                    {
                        onDisconnected();

                    });

                }

            }).Start();

            return true;
        }
        return false;
    }

  


    public bool WasConnectedToARoom()
    {
        return !string.IsNullOrEmpty(_lastConnectedRoom);
    }

    public void Disconnect()
    {
        _lastConnectedRoom = null;
        _lastconnectedIP = null;
        clients.Clear();
        client.DisconnectAll();
    }

    public bool FetchRoomList()
    {
        return FetchRoomList(currentRoomFilter);
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
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
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
                
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
            }
            return true;
        }
        return false;
    }

    public bool JoinRoom(string roomid,string password = null)
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

                peer.Send(writer, DeliveryMethod.ReliableOrdered);
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
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
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
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
            }
            return true;
        }
        return false;
    }

    public bool RejoinLastRoom()
    {
        if (isConnected && !isInActiveRoom)
        {
            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_REJOIN_ROOM);
                writer.Put(_lastConnectedRoom);

                peer.Send(writer, DeliveryMethod.ReliableOrdered);
            }
            return true;
        }
        return false;
    }

    public bool JoinRoomOrCreateIfNotExist(string roomid,int maxPlayers = 1000)
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
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
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
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
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
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
            }
            return true;
        }
        return false;
    }


   

    public bool RaiseEventOnAll(ushort eventCode,LNSWriter m_writer, DeliveryMethod deliveryMethod)
    {
        if (isConnected && isInActiveRoom)
        {

            writer.Reset();
            writer.Put(LNSConstants.SERVER_EVT_RAW_DATA_NOCACHE);
            writer.Put(eventCode);
            writer.Put(m_writer.Data,0,m_writer.Length);

            peer.Send(writer, deliveryMethod);

           
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
    public bool RaiseEventOnNearby(ushort eventCode,Vector2 position,float extends,LNSWriter m_writer, DeliveryMethod deliveryMethod)
    {
        if (isConnected && isInActiveRoom)
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
            peer.Send(writer, deliveryMethod);

           
            return true;
        }
        return false;
    }


    //public bool RaiseEventOnAllAndCache(ushort eventCode, LNSWriter m_writer)
    //{
    //    if (isConnected && isInActiveRoom)
    //    {

    //        new Thread(() =>
    //        {
    //            lock (thelock)
    //            {
    //                writer.Reset();
    //                writer.Put(LNSConstants.SERVER_EVT_RAW_DATA_CACHE);
    //                writer.Put(eventCode);
    //                writer.Put(m_writer.Data);
    //                peer.Send(writer,DeliveryMethod.ReliableOrdered);
    //            }
    //        }).Start();
    //        return true;
    //    }
    //    return false;
    //}

    //public bool RaiseEventRemoveMyCache()
    //{
    //    if (isConnected && isInActiveRoom)
    //    {

    //        new Thread(() =>
    //        {
    //            lock (thelock)
    //            {
    //                //writer.Reset();
    //                //writer.Put(LNSConstants.SERVER_EVT_RAW_DATA_CACHE);
    //                //writer.Put(eventCode);
    //                //writer.Put(m_writer.Data);
    //                //peer.Send(writer, DeliveryMethod.ReliableOrdered);
    //            }
    //        }).Start();
    //        return true;
    //    }
    //    return false;
    //}

    //public bool RaiseEventRemoveAllCache()
    //{
    //    if (isConnected && isInActiveRoom && isLocalPlayerMasterClient)
    //    {

    //        new Thread(() =>
    //        {
    //            lock (thelock)
    //            {
    //                writer.Reset();
    //                writer.Put(LNSConstants.SERVER_EVT_REMOVE_ALL_CACHE);
    //                peer.Send(writer, DeliveryMethod.ReliableOrdered);
    //                //writer.Put(eventCode);
    //                //writer.Put(m_writer.Data);

    //            }
    //        }).Start();
    //        return true;
    //    }
    //    return false;
    //}




    public bool RaiseEventOnClient(LNSClient client, ushort eventCode, LNSWriter m_writer, DeliveryMethod deliveryMethod)
    {
        return RaiseEventOnClient(client.id, eventCode, m_writer, deliveryMethod);
    }

    public bool RaiseEventOnMasterClient (ushort eventCode, LNSWriter m_writer, DeliveryMethod deliveryMethod)
    {
        if(isLocalPlayerMasterClient)
        {
            return false;
        }
        return RaiseEventOnClient(_lastConnectedRoomMasterClient, eventCode, m_writer, deliveryMethod);
    }

    public bool RaiseEventOnClient(string clientid,ushort eventCode, LNSWriter m_writer, DeliveryMethod deliveryMethod)
    {
        if (isConnected && isInActiveRoom)
        {

            new Thread(() =>
            {
                lock (thelock)
                {
                    writer.Reset();
                    writer.Put(LNSConstants.SERVER_EVT_RAW_DATA_TO_CLIENT);
                    writer.Put(clientid);
                    writer.Put(eventCode);
                    if (m_writer.Data.Length > 0)
                    {
                        writer.Put(m_writer.Data);
                    }
                    peer.Send(writer, deliveryMethod);
                    //peer.Send(writer, deliveryMethod);
                }
            }).Start();
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
                new Thread(() =>
                {
                    lock (thelock)
                    {
                        writer.Reset();
                        writer.Put(LNSConstants.SERVER_EVT_RAW_DATA_CACHE);
                        writer.Put(key);
                        writer.Put(m_writer.Data);
                       
                        peer.Send(writer, DeliveryMethod.ReliableOrdered);
                        //peer.Send(writer, deliveryMethod);
                    }
                }).Start();
                return true;
            }
            else
            {
                throw new Exception("Only master client can send Cached Data to server");
            }

           
        }
        return false;
        
    }

    public bool SendCachedDataToAll(string key, byte [] rawData)
    {
        if (isConnected && isInActiveRoom)
        {
            if (isLocalPlayerMasterClient)
            {
                new Thread(() =>
                {
                    lock (thelock)
                    {
                        writer.Reset();
                        writer.Put(LNSConstants.SERVER_EVT_RAW_DATA_CACHE);
                        writer.Put(key);
                        writer.Put(rawData);

                        peer.Send(writer, DeliveryMethod.ReliableOrdered);
                        //peer.Send(writer, deliveryMethod);
                    }
                }).Start();
                return true;
            }
            else
            {
                throw new Exception("Only master client can send Cached Data to server");
            }


        }
        return false;

    }


    public void DisconnectFromRoom()
    {
        if (isConnected && isInActiveRoom)
        {
            _lastConnectedRoom = null;
            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_LEAVE_ROOM);
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
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


    private void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader packetReader, DeliveryMethod deliveryMethod)
    {
        byte clientInstruction = packetReader.GetByte();

        if(clientInstruction == LNSConstants.CLIENT_EVT_ROOM_LIST)
        {
            if(roomList == null)
            {
                roomList = new LNSRoomList();
            }
            JsonUtility.FromJsonOverwrite(packetReader.GetString(), roomList);
            if (onRoomListReceived != null)
            {
                threadDispatcher.Add(() => onRoomListReceived(roomList));
            }
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_CREATED)
        {
            isInActiveRoom = true;
            _lastConnectedRoom = packetReader.GetString();

            
            if (onRoomCreated != null)
            {
                threadDispatcher.Add(()=>onRoomCreated());
            }
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_DISCONNECTED)
        {
            if (onDisconnectedFromRoom != null)
            {
                threadDispatcher.Add(() => onDisconnectedFromRoom());
            }
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_FAILED_CREATE)
        {
            byte reason = packetReader.GetByte();
            if (onRoomCreateFailed != null)
            {
                threadDispatcher.Add(() => onRoomCreateFailed((ROOM_FAILURE_CODE) reason));
            }
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_FAILED_JOIN)
        {
            byte reason = packetReader.GetByte();
            if (onRoomJoinFailed != null)
            {
                threadDispatcher.Add(() => onRoomJoinFailed((ROOM_FAILURE_CODE) reason));
            }
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_FAILED_REJOIN)
        {
            byte reason = packetReader.GetByte();
            if (onRoomRejoinFailed != null)
            {
                threadDispatcher.Add(() => onRoomRejoinFailed((ROOM_FAILURE_CODE)reason));
            }
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_JOINED)
        {
            isInActiveRoom = true;
            _lastConnectedRoom = packetReader.GetString();
           
            if (onRoomJoined != null)
            {
                threadDispatcher.Add(() => onRoomJoined());
            }
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_REJOINED)
        {
            isInActiveRoom = true;
            _lastConnectedRoom = packetReader.GetString();
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
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_MASTERCLIENT_CHANGED)
        {
            _lastConnectedRoomMasterClient = packetReader.GetString();
            localClient.isMasterClient = isLocalPlayerMasterClient = (_lastConnectedRoomMasterClient == id);         
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
                            if (clients[i].id == _lastConnectedRoomMasterClient)
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
                }catch(System.Exception ex)
                {
                    Debug.LogError(ex.Message + " " + ex.StackTrace);
                }
                
            }
            
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_PLAYER_CONNECTED)
        {
            string client_id = packetReader.GetString();
            string client_displayName = packetReader.GetString();
            CLIENT_PLATFORM client_platform = (CLIENT_PLATFORM) packetReader.GetByte();
            int  client_netID = packetReader.GetInt();
            LNSClient client = null;
            for(int i=0;i<clients.Count;i++)
            {
                if(clients[i].id == client_id)
                {
                    client = clients[i];
                    client.displayName = client_displayName;
                    client.platform = client_platform;
                    client.networkID = client_netID;
                    break;
                }
            }

            if(client == null)
            {
                client = new LNSClient();
                client.id = client_id;
                client.displayName = client_displayName;
                client.platform = client_platform;
                client.networkID = client_netID;

                clients.Add(client);
            }
            client.isConnected = true;

            if (onPlayerConnected != null)
            {
                threadDispatcher.Add(() =>
                {
                    onPlayerConnected(client);
                });
            }
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_PLAYER_DISCONNECTED)
        {
            string client_id = packetReader.GetString();
            
            for (int i = 0; i < clients.Count; i++)
            {
               
                if (clients[i].id == client_id)
                {
                    clients[i].isConnected = false;
                    if (onPlayerDisconnected != null)
                    {
                        threadDispatcher.Add(() =>
                        {
                            onPlayerDisconnected(clients[i]);
                        });
                    }
                    return;
                }
            }
        }
        else if(clientInstruction == LNSConstants.CLIENT_EVT_ROOM_CACHE_DATA)
        {
            LNSReader reader = LNSReader.GetFromPool();
            string key = packetReader.GetString();
            byte[] data = packetReader.GetRemainingBytes();
            reader.SetSource(data);

            if(persistentData.ContainsKey(key))
            {
                persistentData[key] = data;
            }
            else
            {
                persistentData.Add(key, data);
            }

            if (dataReceiver != null)
            {
                threadDispatcher.Add(() =>
                {
                    try
                    {
                        dataReceiver.OnCachedDataReceived(key, reader);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError(ex.Message + " " + ex.StackTrace);
                    }
                    reader.Recycle();


                });
            }
            

        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_RAW)
        {
            string fromid = packetReader.GetString();

            for (int i = 0; i < clients.Count; i++)
            {
                if(fromid == clients[i].id)
                {
                    
                    if (dataReceiver != null)
                    {
                        ushort eventCode = packetReader.GetUShort();
                        LNSReader reader = LNSReader.GetFromPool();

                       
                        LNSClient fromClient = clients[i];
                        DeliveryMethod _deliveryMethod = deliveryMethod;

                        reader.SetSource(packetReader.GetRemainingBytes());

                        threadDispatcher.Add(() =>
                        {
                            try
                            {
                                dataReceiver.OnEventRaised(fromClient, eventCode, reader, _deliveryMethod);
                            }
                            catch(System.Exception ex) {
                                Debug.LogError(ex.Message + " "+ex.StackTrace);
                            }
                            reader.Recycle();


                        });

                       
                    }
                    break;
                }
            }
          
        }
        packetReader.Recycle();
    }

    private void Listener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        bool wasConnected = isConnected;
        localClient.isConnected = isConnected = isInActiveRoom = false;
        if (wasConnected)
        {
            if(onDisconnected != null)
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
            catch(System.Exception ex)
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
        Debug.Log(peer.EndPoint);
        localClient.isConnected = isConnected = true;
        if(onConnected != null)
        {
            DispatchToMainThread(() =>
            {
                onConnected();
            });
        }
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
            if(action != null)
            {
                action();
            }
            
        });
    }

    public void Dispose()
    {
        if(client != null)
        {
            client.DisconnectAll();
        }
    }
}
