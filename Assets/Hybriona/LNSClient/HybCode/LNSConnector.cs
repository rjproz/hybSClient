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

   

    private string securityKey;
    private LNSMainThreadDispatcher threadDispatcher;
    private ILNSDataReceiver dataReceiver;
    private NetManager client;
    private NetPeer peer;
    private NetDataWriter writer;

    private object thelock = new object();


    private string _lastconnectedIP;
    private int _lastconnectedPort;
    private string _lastConnectedRoom;
    public LNSConnector(string securityKey, ILNSDataReceiver dataReceiver)
    {
        
        this.securityKey = securityKey;
        this.dataReceiver = dataReceiver;
        this.threadDispatcher = LNSMainThreadDispatcher.GetInstance();
        writer = new NetDataWriter();

        EventBasedNetListener listener = new EventBasedNetListener();
        client = new NetManager(listener);

        listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;
    }

    public void Connect(string ip, int port)
    {
        _lastconnectedIP = ip;
        _lastconnectedPort = port;
        new Thread(() =>
        {
            bool connectionFailed = false;
            
            
            client.Start();
            try
            {
                peer =  client.Connect(ip, port,this.securityKey);
                
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


    public void ReconnectAndRejoin(int retries = 20)
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
                peer = client.Connect(_lastconnectedIP, _lastconnectedPort, this.securityKey);
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
    }

    public void Disconnect()
    {
        isConnected = false;
        clients.Clear();
        client.DisconnectAll();
    }


    public void CreateRoom(string roomid,bool isPublic = true,string password = null, int maxPlayers = 1000)
    {
        _lastConnectedRoom = roomid;
        //TODO Check if player is already in a room
        lock (thelock)
        {
            writer.Reset();
            writer.Put(LNSConstants.SERVER_EVT_CREATE_ROOM);
            writer.Put(id);
            writer.Put(displayName);
            writer.Put(roomid);
            writer.Put(isPublic);
            if(string.IsNullOrEmpty(password))
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
    }

    public void JoinRoom(string roomid,string password = null)
    {
        _lastConnectedRoom = roomid;
        //TODO Check if player is already in a room
        lock (thelock)
        {
            writer.Reset();
            writer.Put(LNSConstants.SERVER_EVT_JOIN_ROOM);
            writer.Put(id);
            writer.Put(displayName);
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
    }

    protected void RejoinRoom()
    {
        lock (thelock)
        {
            writer.Reset();
            writer.Put(LNSConstants.SERVER_EVT_REJOIN_ROOM);
            writer.Put(id);
            writer.Put(displayName);
            writer.Put(_lastConnectedRoom);

            client.SendToAll(writer, DeliveryMethod.ReliableOrdered);
        }
    }

    public void JoinRoomOrCreateIfNotExist(string roomid,int maxPlayers = 1000)
    {
        _lastConnectedRoom = roomid;
        //TODO Check if player is already in a room
        lock (thelock)
        {
            writer.Reset();
            writer.Put(LNSConstants.SERVER_EVT_CREATE_OR_JOIN_ROOM);
            writer.Put(id);
            writer.Put(displayName);
            writer.Put(roomid);
            writer.Put(maxPlayers);
            client.SendToAll(writer, DeliveryMethod.ReliableOrdered);
        }
    }

    public void LockRoom()
    {
        //TODO Check if player is already in a room
        lock (thelock)
        {
            writer.Reset();
            writer.Put(LNSConstants.SERVER_EVT_LOCK_ROOM);        
            client.SendToAll(writer, DeliveryMethod.ReliableOrdered);
        }
    }

    public void UnLockRoom()
    {
        //TODO Check if player is already in a room
        lock (thelock)
        {
            writer.Reset();
            writer.Put(LNSConstants.SERVER_EVT_UNLOCK_ROOM);
            client.SendToAll(writer, DeliveryMethod.ReliableOrdered);
        }
    }



    public void SendData(NetDataWriter m_writer, DeliveryMethod deliveryMethod)
    {
        if (!isConnected)
            return;

        new Thread(() =>
        {
            lock (thelock)
            {
                writer.Reset();
                writer.Put(LNSConstants.SERVER_EVT_RAW_DATA_NOCACHE);
                writer.Put(m_writer.Data);
                client.SendToAll(writer, deliveryMethod);
            }
        }).Start();
    }



    public void DisconnectFromRoom()
    {
        //TODO Check if player is already in a room
        lock (thelock)
        {
            writer.Reset();
            writer.Put(LNSConstants.SERVER_EVT_LEAVE_ROOM);      
            client.SendToAll(writer, DeliveryMethod.ReliableOrdered);
        }
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
            if (onRoomCreateFailed != null)
            {
                threadDispatcher.Add(() => onRoomCreateFailed());
            }
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_FAILED_JOIN)
        {
            byte reason = reader.GetByte();
            if (onRoomJoinFailed != null)
            {
                threadDispatcher.Add(() => onRoomJoinFailed((LNSConstants.ROOM_FAILURE_CODE) reason));
            }
        }
        else if (clientInstruction == LNSConstants.CLIENT_EVT_ROOM_FAILED_REJOIN)
        {
            byte reason = reader.GetByte();
            if (onRoomRejoinFailed != null)
            {
                threadDispatcher.Add(() => onRoomRejoinFailed((LNSConstants.ROOM_FAILURE_CODE)reason));
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

            LNSClient client = null;
            for(int i=0;i<clients.Count;i++)
            {
                if(clients[i].id == client_id)
                {
                    client = clients[i];
                    client.displayName = client_displayName;
                    break;
                }
            }

            if(client == null)
            {
                client = new LNSClient(client_id);
                client.displayName = client_displayName;
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
                        threadDispatcher.Add(() =>
                        {
                            try
                            {
                                dataReceiver.OnDataReceived(clients[i], reader);
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
