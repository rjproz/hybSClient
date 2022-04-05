using LiteNetLib;
using UnityEngine;
public class LNSBaseNetSync : MonoBehaviour
{
    public float sendTickRate = 30;
    public string instanceId;
    public DeliveryMethod deliveryMethod = DeliveryMethod.Sequenced;

    public LNSClient assignedClient { get; set; }
    private float lastTimeUpdated;
    private float updateDelay;
    protected float timeElaspedSinceLastPacket;
    protected float timePacketReceived;
    public virtual void Write(LNSWriter writer)
    {
        writer.Put(transform.position);
        writer.Put(transform.eulerAngles);
    }

    public virtual void ReadAndApply(LNSReader reader)
    {
        Vector3 position = reader.GetVector3();
        Vector3 eulers = reader.GetVector3();
        transform.position = position;
        transform.eulerAngles = eulers;
       
    }


    public bool CanUpdate()
    {
        
        if (sendTickRate == 0)
        {
            return true;
        }
        updateDelay = 1f / (float)sendTickRate;
        return Time.time - lastTimeUpdated >= updateDelay;
    }

    public void ConsumeUpdate()
    {
        lastTimeUpdated = Time.time;
    }

    private void Start()
    {
        if (sendTickRate == 0)
        {
            updateDelay = 0;
        }
       
    }

    public void SetTimePackageReceived(float time)
    {
        timeElaspedSinceLastPacket = (time - timePacketReceived);
        timePacketReceived = time;
    }
   
}
