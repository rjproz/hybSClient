using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LNSClientParameters 
{
    public string id { get; private set; }
    public string displayName { get; private set; }


    public LNSClientParameters(string clientid,string displayName = null)
    {
        this.id = clientid;
        this.displayName = displayName;
    }

}
