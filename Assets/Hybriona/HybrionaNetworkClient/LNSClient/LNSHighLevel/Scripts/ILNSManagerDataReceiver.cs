using System.Collections;
using System.Collections.Generic;
using LiteNetLib;
using UnityEngine;

public interface ILNSManagerDataReceiver 
{
    void OnEventRaised(LNSClient from, ushort eventCode, LNSReader reader, DeliveryMethod deliveryMethod);
    void OnCachedDataReceived(string ofKey, byte[] rawData);
}
