using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TreeNetwork : MonoBehaviour
{
    // Start is called before the first frame update
    public float growthRate = 0;
    public float maxHeight = 10;
    public float maxWidth = 5;

    public string id;
   

    private bool canGrow = false;
    private LNSWriter commonWriter;
    public void StartGrow()
    {
        commonWriter = new LNSWriter();
        canGrow = true;

        StartCoroutine(SendData());
    }

    
    [ContextMenu("DeleteTree")]
    public void DeleteTree()
    {
        if(commonWriter == null)
        {
            commonWriter = new LNSWriter();
        }
        commonWriter.Reset();
        commonWriter.Put(id);
        LNSManager.RaiseEventOnMasterClient(13, commonWriter, LiteNetLib.DeliveryMethod.ReliableOrdered);
    }

    IEnumerator SendData()
    {
        WaitForSeconds wait = new WaitForSeconds(1);
        while(true)
        {
            if(LNSManager.connector.isConnected)
            {
                commonWriter.Reset();
                commonWriter.Put(id);
                commonWriter.Put(transform.position);
                commonWriter.Put(transform.localScale);
                LNSManager.RaiseEventOnAll(11, commonWriter, LiteNetLib.DeliveryMethod.ReliableSequenced);
                
            }
            yield return wait;
        }
    }

    public void Update()
    {
       
        if (canGrow)
        {

            var currentHeight = transform.localScale.y;

            if (currentHeight < maxHeight)
            {
                float newHeight = currentHeight + Time.deltaTime * growthRate;
                float newWidth = Mathf.Lerp(1, maxWidth, newHeight / maxHeight);
                transform.localScale = new Vector3(newWidth, newHeight, newWidth);
            }
        }
    }
}
