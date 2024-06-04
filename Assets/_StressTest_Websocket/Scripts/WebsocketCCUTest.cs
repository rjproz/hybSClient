using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WebsocketCCUTest : MonoBehaviour
{
    // Start is called before the first frame update
    public bool local;
    public int ccu;
    public Text messageText;

    public TMPro.TMP_InputField ccuField;
    public Button startButton;


    private FakeClientWebsocket receiver;
    private List<FakeClientWebsocket> clients = new List<FakeClientWebsocket>();

    private void Start()
    {
        startButton.onClick.AddListener(() =>
        {
            startButton.interactable = false;
            ccu = int.Parse(ccuField.text);
            StartCoroutine(StartProcess());
        });
    }

    IEnumerator StartProcess()
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
            FakeClientWebsocket client = new FakeClientWebsocket(i,ip, 10010);
            clients.Add(client);
            if (i % 10 == 0)
            {
               
                yield return null;
                yield return null;
                yield return null;
            }

            

            if( i == 0)
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
        foreach (var client in clients)
        {
            client.Disconnect();
        }
    }

    private void Update()
    {
        for (int i = 0; i < clients.Count; i++)
        {

            clients[i].Update();
        }
    }
}
