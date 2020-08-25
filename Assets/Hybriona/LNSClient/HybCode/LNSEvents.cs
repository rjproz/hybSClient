﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void OnConnected();
public delegate void OnFailedToConnect();
public delegate void OnDisconnected();

public delegate void OnRoomCreated();
public delegate void OnRoomJoined();
public delegate void OnRoomRejoined();
public delegate void OnDisconnectedFromRoom();
public delegate void OnRoomCreateFailed(ROOM_FAILURE_CODE code);
public delegate void OnRoomJoinFailed(ROOM_FAILURE_CODE code);
public delegate void OnRoomRejoinFailed(ROOM_FAILURE_CODE code);

public delegate void OnMasterClientUpdated(string clientId);
public delegate void OnPlayerConnected(LNSClient client);
public delegate void OnPlayerDisconnected(LNSClient client);
