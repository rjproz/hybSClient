using System.Collections;
using System.Collections.Generic;
using LiteNetLib;
using UnityEngine;

public interface ILNSDataReceiver
{
     void OnEventRaised(LNSClient from,ushort eventCode, LNSReader reader, DeliveryMethod deliveryMethod);
    
}
