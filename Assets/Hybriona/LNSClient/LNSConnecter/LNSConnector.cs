using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

public class LNSConnector
{
    public string id { get; set; }
    public string displayName { get; set; }

    public bool isConnected { get; private set; }
    public bool isInActiveRoom { get; private set; } = false;

    public List<LNSClient> clients { get; private set; } = new List<LNSClient>();
    public bool isLocalPlayerMasterClient { get; private set; } = false;

    public OnConnected onConnected;
    public OnFailedToConnect onFailedToConnect;
    public OnDisconnected onDisconnected;

    public OnRoomCreated onRoomCreated;
    public OnRoomJoined onRoomJoined;
    public OnRoomRejoined onRoomRejoined;
    public OnDisconnectedFromRoom onDisconnectedFromRoom;
    public OnRoomCreateFailed onRoomCreateFailed;
    public OnRoomJoinFailed onRoomJoinFailed;
    public OnRoomRejoinFailed onRoomRejoinFailed;

    public OnMasterClientUpdated onMasterClientUpdated;
    public OnPlayerConnected onPlayerConnected;
    public OnPlayerDisconnected onPlayerDisconnected;

   

    private LNSConnectSettings settings;
    private LNSMainThreadDispatcher threadDispatcher;
    private ILNSDataReceiver dataReceiver;
    private NetManager client;
    private NetPeer peer;
    private NetDataWriter writer;

    private object thelock = new object();


    private string _lastconnectedIP;
    private int _lastconnectedPort;
    private string _lastConnectedRoom;
    public LNSConnector(LNSConnectSettings settings, ILNSDataReceiver dataReceiver)
    {
        this.settings = settings;
       

        this.dataReceiver = dataReceiver;
        this.threadDispatcher = LNSMainThreadDispatcher.GetInstance();
        writer = new NetDataWriter();
        this.settings.Validate();

        EventBasedNetListener listener = new EventBasedNetListener();
        client = new NetManager(listener);

        listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;
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
            bool connectionFailed = false;
            
            
            client.Start();
            try
            {
                peer =  client.Connect(ip, port,this.settings.serverSecurityKey);
                
                if (peer != null )
                {
                    while(peer.ConnectionState == ConnectionState.Outgoing)
                    {
                        Thread.Sleep(10);
                    }

                    if(peer.ConnectionState == ConnectionState.Connected)
                    {
                        isConnected = true;
                        threadDispatcher.Add(() =>
                        {
                            if (onConnected != null)
                            {
                                onConnected();
                            }
                        });
                       
                    }
                    else
                    {
                        connectionFailed = true;
                       
                    }
                   
                }
                else
                {
                    connectionFailed = false;
                }
            }
            catch
            {
                connectionFailed = false;
                
            }
            
            if(connectionFailed)
            {
                isConnected = false;
                threadDispatcher.Add(() =>
                {
                    if (onFailedToConnect != null)
                    {
                        onFailedToConnect();
                    }
                });
                return;
            }




            Update();





        }).Start();
        return false;
    }

    protected void Update()
    {
        new Thread(() =>
        {
            while (isConnected)
            {
                if (peer.ConnectionState == ConnectionState.Disconnected)
                {
                    threadDispatcher.Add(() =>
                    {
                        isConnected = false;
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
        }).Start();

    }


    public bool ReconnectAndRejoin(int retries = 20)
    {
        if (!isConnected && !isInActiveRoom && WasConnectedToARoom())
        {
            //TODO Reconnect logic
            new Thread(() =>
            {
                for (int i = 0; i < retries; i++)
                {
                    try
                    {
                        client.Start();
                    }
                    catch { }

                    Debug.Log("Reconnecting: Begin");
                    client.Flush();
                    peer = client.Connect(_lastconnectedIP, _lastconnectedPort, this.settings.serverSecurityKey);
                    while (peer.ConnectionState == ConnectionState.Outgoing)
                    {
                        Thread.Sleep(10);
                    }

                    if (peer.ConnectionState == ConnectionState.Connected)
                    {
                        Debug.Log("Reconnecting : Connected");
                        isConnected = true;
                        Debug.Log("Reconnecting : Rejoining Room");
                        Update();
                        RejoinRoom();
                        return;
                    }
                    Debug.Log("Reconnecting : Not Connected");
                    Thread.Sleep(5000);
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
        isConnected = false;
        clients.Clear();
        client.DisconnectAll();
    }


    public bool CreateRoom(string roomid,bool isPublic = true,string password = null, int maxPlayers = 1000)
    {
        if (isConnected && !isInActiveRoom)
        {
            _lastConnectedRoom = roomid;
            //TODO Check if player is already in a room
            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_CREATE_ROOM);
                WritePlayerData(writer);


                writer.Put(roomid);
                writer.Put(isPublic);
                if (string.IsNullOrEmpty(password))
                {
                    writer.Put(false);
                }
                else
                {
                    writer.Put(true);
                    writer.Put(password);
                }


                writer.Put(maxPlayers);
                client.SendToAll(writer, DeliveryMethod.ReliableOrdered);
            }
            return true;
        }
        return false;
    }

    public bool JoinRoom(string roomid,string password = null)
    {
        if (isConnected && !isInActiveRoom)
        {
            _lastConnectedRoom = roomid;
            //TODO Check if player is already in a room
            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_JOIN_ROOM);
                WritePlayerData(writer);
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

                client.SendToAll(writer, DeliveryMethod.ReliableOrdered);
            }
            return true;
        }
        return false;
    }

    protected bool RejoinRoom()
    {
        if (isConnected && !isInActiveRoom)
        {
            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_REJOIN_ROOM);
                WritePlayerData(writer);
                writer.Put(_lastConnectedRoom);

                client.SendToAll(writer, DeliveryMethod.ReliableOrdered);
            }
            return true;
        }
        return false;
    }

    public bool JoinRoomOrCreateIfNotExist(string roomid,int maxPlayers = 1000)
    {
        if (isConnected && !isInActiveRoom)
        {
            _lastConnectedRoom = roomid;
            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_CREATE_OR_JOIN_ROOM);
                WritePlayerData(writer);
                writer.Put(roomid);
                writer.Put(maxPlayers);
                client.SendToAll(writer, DeliveryMethod.ReliableOrdered);
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
                client.SendToAll(writer, DeliveryMethod.ReliableOrdered);
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
                client.SendToAll(writer, DeliveryMethod.ReliableOrdered);
            }
            return true;
        }
        return false;
    }



    public bool RaiseEvent(int eventID,NetDataWriter m_writer, DeliveryMethod deliveryMethod)
    {
        if (isConnected && isInActiveRoom)
        {

            new Thread(() =>
            {
                lock (thelock)
                {
                    writer.Reset();
                    writer.Put(LNSConstants.SERVER_EVT_RAW_DATA_NOCACHE);
                    writer.Put(eventID);
                    writer.Put(m_writer.Data);
                    client.SendToAll(writer, deliveryMethod);
                }
            }).Start();
            return true;
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
                client.SendToAll(writer, DeliveryMethod.ReliableOrdered);
            }
           
        }
    }


    protected void WritePlayerData(NetDataWriter writer)
    {
        writer.Put(id);
        writer.Put(displayName);
        writer.Put(this.settings.gameKey);
        writer.Put(this.settings.gameVersion);
        writer.Put(this.settings.platform);
    }


    private void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
    {
        byte clientInstruction = reader.GetByte();
        if(clientInstruction == LNSConstants.CLIENT_EVT_ROOM_CREATED)
        {
            isInActiveRoom = true;
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
            byte reason = reader.GetByte();
            if (onRoomCreateFailed != null)
            {
                threadDispatcher.Add(() => onRoomCreateFailed((ROOM_FAILURE_CODE) reason));
            }
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_FAILED_JOIN)
        {
            byte reason = reader.GetByte();
            if (onRoomJoinFailed != null)
            {
                threadDispatcher.Add(() => onRoomJoinFailed((ROOM_FAILURE_CODE) reason));
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
            if (onRoomJoined != null)
            {
                threadDispatcher.Add(() => onRoomJoined());
            }
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_REJOINED)
        {
            isInActiveRoom = true;
            if (onRoomRejoined != null)
            {
                threadDispatcher.Add(() => onRoomRejoined());
            }
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_MASTERCLIENT_CHANGED)
        {
            string masterclientid = reader.GetString();
            isLocalPlayerMasterClient = (masterclientid == id);
          
            if(onMasterClientUpdated != null)
            {
                threadDispatcher.Add(() =>
                {
                    onMasterClientUpdated(masterclientid);
                });
            }
            
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_PLAYER_CONNECTED)
        {
            string client_id = reader.GetString();
            string client_displayName = reader.GetString();
            string client_version = reader.GetString();
            CLIENT_PLATFORM client_platform = (CLIENT_PLATFORM) reader.GetByte();

            LNSClient client = null;
            for(int i=0;i<clients.Count;i++)
            {
                if(clients[i].id == client_id)
                {
                    client = clients[i];
                    client.displayName = client_displayName;
                    client.gameVersion = client_version;
                    client.platform = client_platform;
                    break;
                }
            }

            if(client == null)
            {
                client = new LNSClient(client_id);
                client.displayName = client_displayName;
                client.gameVersion = client_version;
                client.platform = client_platform;

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
            string client_id = reader.GetString();
            
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
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_RAW)
        {
            string fromid = reader.GetString();
            
            for (int i = 0; i < clients.Count; i++)
            {
                if(fromid == clients[i].id)
                {
                    if (dataReceiver != null)
                    {
                        DeliveryMethod _deliveryMethod = deliveryMethod;
                        threadDispatcher.Add(() =>
                        {
                            try
                            {
                                dataReceiver.OnEventRaised(clients[i],reader.GetInt(), reader, _deliveryMethod);
                            }
                            catch { }
                            reader.Recycle();
                        });

                        return;
                    }
                    else
                    {
                        break;
                    }
                }
            }
          
        }
        reader.Recycle();
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
}
