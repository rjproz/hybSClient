using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void OnConnected();
public delegate void OnFailedToConnect(CONNECTION_FAILURE_CODE code = CONNECTION_FAILURE_CODE.COULD_NOT_CONNECT_TO_HOST);
public delegate void OnDisconnected();

public delegate void OnRoomListReceived(LNSRoomList roomList);

public delegate void OnRoomExistsResponse(string roomId, bool exists);
public delegate void OnRoomCreated();
public delegate void OnRoomJoined();
public delegate void OnRoomRejoined();
public delegate void OnDisconnectedFromRoom();
public delegate void OnRoomCreateFailed(ROOM_FAILURE_CODE code);
public delegate void OnRoomJoinFailed(ROOM_FAILURE_CODE code);
public delegate void OnRoomRejoinFailed(ROOM_FAILURE_CODE code);
public delegate void OnRandomRoomJoinFailed();

public delegate void OnMasterClientUpdated(LNSClient client);
public delegate void OnPlayerConnected(LNSClient client);
public delegate void OnPlayerDisconnected(LNSClient client);
