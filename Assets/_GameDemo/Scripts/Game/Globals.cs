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

        string id = SystemInfo.deviceUniqueIdentifier;
        if(Application.isEditor)
        {
            id = id + "_editor";
        }
        multiplayerClient.Initialize(id);
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
        if (Globals.Instance.multiplayerClient.connector.WasConnectedToARoom())
        {

            Globals.Instance.multiplayerClient.connector.ReconnectAndRejoin();
        }
    }

    private void OnFailedToConnect(CONNECTION_FAILURE_CODE code)
    {
        Debug.LogError("Failed to connect "+ code);
    }

    private void OnConnected()
    {
        Debug.Log("OnConnected");
        connectUI.EnableInput();
    }
}
