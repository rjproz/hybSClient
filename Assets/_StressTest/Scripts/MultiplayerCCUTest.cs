using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerCCUTest : MonoBehaviour
{
    // Start is called before the first frame update
    public int ccu;
    public Text messageText;

    private FakeClient sender;
    private FakeClient receiver;
    IEnumerator Start()
    {
        Application.targetFrameRate = 60;
        string idoffset = Random.Range(1, 999999)+"" + Random.Range(1, 999999); 
        for(int i=0;i<ccu;i++)
        {
            FakeClient client = new FakeClient("45.55.33.88", 10002, string.Format("Agent {0}_{1}", idoffset,(i + 1)));
            if(i % 10 == 0)
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

        receiver.messageReceiver += (string message) =>
        {
            messageText.text = message;
        };

        while(true)
        {
            yield return null;
            //yield return new WaitForSeconds(Random.Range(1f,2f));
            sender.SendData();
        }
    }

   
}
