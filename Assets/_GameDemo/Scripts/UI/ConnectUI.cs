using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConnectUI : BaseUI
{
    public CanvasGroup canvasGroup;
    public InputField userNameText;
    public InputField roomIdText;
    public InputField passwordText;
    public Text statusText;

    public void Show()
    {
        if(GetCanvas() != null)
        {
            GetCanvas().enabled = true;
        }
        EnableInput();
        statusText.text = "";


        Globals.Instance.multiplayerClient.connector.onRoomCreated = OnRoomCreated;
        Globals.Instance.multiplayerClient.connector.onRoomCreateFailed = OnRoomCreateFailed;
        Globals.Instance.multiplayerClient.connector.onRoomJoined = OnRoomJoined;
        Globals.Instance.multiplayerClient.connector.onRoomJoinFailed = OnRoomJoinFailed;
    }

    private void OnRoomJoinFailed(ROOM_FAILURE_CODE code)
    {
        Debug.Log("OnRoomJoinFailed "+ code.ToString());
        EnableInput("Room doesn't exist "+code.ToString());
    }

    private void OnRoomJoined()
    {
        Debug.Log("OnRoomJoined");
        Hide();
    }

    private void OnRoomCreateFailed(ROOM_FAILURE_CODE code)
    {
        Debug.Log("OnRoomCreateFailed "+ code);
        EnableInput("Failed to create room! "+ code);
    }

    private void OnRoomCreated()
    {
        Debug.Log("OnRoomCreated");
        Hide();
    }

    

    public void Hide()
    {
        if (GetCanvas() != null)
        {
            GetCanvas().enabled = false;
        }
    }

    public void Processing()
    {
        canvasGroup.interactable = false;
        statusText.text = "PROCESSING..";
;   }

    public void EnableInput(string status = "")
    {
        statusText.text = status;
        canvasGroup.interactable = true;
    }

    public void OnCreateRoomClicked()
    {

        Processing();
        Globals.Instance.multiplayerClient.connector.SetDisplayName(userNameText.text);
        Globals.Instance.multiplayerClient.connector.CreateRoom(roomIdText.text,true, passwordText.text);
    }

    public void OnJoinRoomClicked()
    {
        Processing();
        Globals.Instance.multiplayerClient.connector.SetDisplayName(userNameText.text);
        Globals.Instance.multiplayerClient.connector.JoinRoom(roomIdText.text, passwordText.text);
    }
}
