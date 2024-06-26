﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerCCUTest : MonoBehaviour
{
    // Start is called before the first frame update
    public bool local;
    public int ccu;
    public Text messageText;

    private FakeClient sender;
    private FakeClient receiver;
    private List<FakeClient> clients = new List<FakeClient>();
    IEnumerator Start()
    {
        Application.targetFrameRate = 30;
        string idoffset = Random.Range(1, 999999)+"" + Random.Range(1, 999999); 
        for(int i=0;i<ccu;i++)
        {
            string ip = "vps.hybriona.com";
            if(local)
            {
                ip = "localhost";
            }
            FakeClient client = new FakeClient(ip, 10002, string.Format("Agent {0}_{1}", idoffset,(i + 1)));
            clients.Add(client);
            if (i % 10 == 0)
            {
               
                yield return null;
                yield return null;
                yield return null;
            }

            if(i == 0)
            {
                sender = client;
            }

            if( i == 10)
            {
                receiver = client;
            }
        }


        if (receiver != null)
        {
            receiver.messageReceiver += (string message) =>
            {
                messageText.text = message;
            };
        }

        while(true)
        {
            for (int i = 0; i < clients.Count; i++)
            {
                yield return null;
                //yield return new WaitForSeconds(Random.Range(1f,2f));
                clients[i].SendData();
            }
        }
    }

    private void OnDestroy()
    {
        foreach(var client in clients)
        {
            client.Disconnect();
        }
    }
}
