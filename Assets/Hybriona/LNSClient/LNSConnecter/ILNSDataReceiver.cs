using System.Collections;
using System.Collections.Generic;
using LiteNetLib;
using UnityEngine;

public interface ILNSDataReceiver
{
     void OnEventRaised(LNSClient from,int eventCode, NetPacketReader reader, DeliveryMethod deliveryMethod);
    
}
