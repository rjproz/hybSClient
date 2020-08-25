using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LNSClient 
{
    public string id { get; private set; }
    public string displayName { get; set; }
    //public bool isMasterClient { get;  set; }
    public bool isConnected { get;  set; }

    public LNSClient(string id)
    {
        this.id = id;
    }
}
