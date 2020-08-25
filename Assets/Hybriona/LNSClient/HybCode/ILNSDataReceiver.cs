using System.Collections;
using System.Collections.Generic;
using LiteNetLib;
using UnityEngine;

public interface ILNSDataReceiver
{
     void OnDataReceived(LNSClient client, NetPacketReader reader);
    
}
