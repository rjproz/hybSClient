using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Globals : MonoBehaviour
{
    public MultiplayerClient multiplayerClient;

    [Header("UI")]
    public ConnectUI connectUI;

    public static Globals Instance { get; private set; }
   

    private void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;
        multiplayerClient.Initialize(SystemInfo.deviceUniqueIdentifier);
        Globals.Instance.multiplayerClient.connector.onConnected = OnConnected;
        Globals.Instance.multiplayerClient.connector.onFailedToConnect = OnFailedToConnect;
        Globals.Instance.multiplayerClient.connector.onDisconnected = OnDisconnected;


        connectUI.Show();
        connectUI.Processing();
        multiplayerClient.Connect();
    }

    private void OnDisconnected()
    {
        
        Debug.LogError("Disconnected from Server");
        Globals.Instance.multiplayerClient.connector.ReconnectAndRejoin();
    }

    private void OnFailedToConnect()
    {
        Debug.LogError("Failed to connect");
    }

    private void OnConnected()
    {
        Debug.Log("OnConnected");
        connectUI.EnableInput();
    }
}
