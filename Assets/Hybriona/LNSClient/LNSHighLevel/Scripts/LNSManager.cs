using System.Collections;
using System.Collections.Generic;
using LiteNetLib;
using UnityEngine;

public class LNSManager : MonoBehaviour,ILNSDataReceiver
{

    protected static LNSManager Instance
    {
        get {
            if (m_instance == null)
            {
                m_instance = GameObject.FindObjectOfType<LNSManager>();
                if (m_instance != null)
                {
                    GameObject.DontDestroyOnLoad(m_instance.gameObject);
                }
                if (m_instance == null)
                {
                    throw new System.Exception("Couldn't find LNSManager");
                }
            }
            return m_instance;
        }
    }



    
    public static LNSWriter DATA_WRITER { get; protected set; }
    public static bool isInitialized { get; private set; }
    protected ILNSManagerDataReceiver dataReceiver;
    protected string serverIP;
    protected int serverPort;
    private long generatedIdCounter = 1;

    private LNSConnector m_connector;
    private List<LNSBaseNetSync> netSyncTransmitters = new List<LNSBaseNetSync>();
    private List<LNSBaseNetSync> netSyncReceivers = new List<LNSBaseNetSync>();
    
    #region Public Methods

    public static void Initialize(LNSClientParameters clientParameters, LNSConnectSettings connectSettings,ILNSManagerDataReceiver dataReceiver)
    {
        Instance.m_connector = null;
        Instance.dataReceiver = dataReceiver;
     
        Instance.m_connector = new LNSConnector(clientParameters, connectSettings, Instance);

        if (DATA_WRITER != null)
        {
            DATA_WRITER.Reset();
        }
        else
        {
            DATA_WRITER = new LNSWriter();
        }

        isInitialized = true;
    }

    public static void Connect(string serverIP, int serverPort)
    {

        Instance.serverIP = serverIP;
        Instance.serverPort = serverPort;
        Instance.m_connector.Connect(Instance.serverIP, Instance.serverPort);
    }

    public static void Connect()
    {
        Instance.m_connector.Connect();
    }

   

    public static void RegisterSyncTransmitter(LNSBaseNetSync transmitter)
    {
        if (!isInitialized)
            return;

        if(Instance.netSyncReceivers.Contains(transmitter))
        {
            Instance.netSyncReceivers.Remove(transmitter);
        }

        if(!Instance.netSyncTransmitters.Contains(transmitter))
        {
            Instance.netSyncTransmitters.Add(transmitter);
        }
    }

    public static void RemoveSyncTransmitter(LNSBaseNetSync transmitter)
    {
        if (!isInitialized)
            return;

        if (Instance.netSyncTransmitters.Contains(transmitter))
        {
            Instance.netSyncTransmitters.Remove(transmitter);
        }
    }

    public static void RegisterSyncReceiver(LNSBaseNetSync receiver)
    {
        if (!isInitialized)
            return;

        if (Instance.netSyncTransmitters.Contains(receiver))
        {
            Instance.netSyncTransmitters.Remove(receiver);
        }

        if (!Instance.netSyncReceivers.Contains(receiver))
        {
            Instance.netSyncReceivers.Add(receiver);
        }
    }

    public static void RemoveSyncReceiver(LNSBaseNetSync receiver)
    {
        if (!isInitialized)
            return;

        if (Instance.netSyncReceivers.Contains(receiver))
        {
            Instance.netSyncReceivers.Remove(receiver);
        }
    }

    public static string GenerateNextId()
    {
        if (!isInitialized)
            return null;
        Instance.generatedIdCounter = Instance.generatedIdCounter + 1;
        return Instance.generatedIdCounter.ToString();
    }


    public static byte[] GetCacheData(string key)
    {
        if (Instance == null || Instance.m_connector == null)
            return null;

        return Instance.m_connector.persistentData[key];

    }

    public int GetMaxSinglePacketSize(DeliveryMethod deliveryMethod)
    {
        if (Instance == null || Instance.m_connector == null)
            return 0;
        return Instance.GetMaxSinglePacketSize(deliveryMethod);
    }

    #region Public properties

    public static LNSConnector connector
    {
        get
        {
            if (Instance == null)
                return null;
            return Instance.m_connector;
        }
    }
    public static LNSClient localClient
    {
        get
        {
            if(Instance.m_connector != null && Instance.m_connector.isConnected)
            {
                return Instance.m_connector.localClient;
            }
            return null;
        }
    }

    public static LNSClient masterClient
    {
        get
        {
            if (Instance.m_connector != null && Instance.m_connector.isConnected)
            {
                return Instance.m_connector.masterClient;
            }
            return null;
        }
    }

    public static int ping
    {
        get
        {
            if(Instance.m_connector != null && Instance.m_connector.isConnected)
            {
                return Instance.m_connector.GetPing();
            }
            return -1;
        }
    }

    public static string localId
    {
        get
        {
            if(Instance.m_connector != null)
            {
                return Instance.m_connector.localClient.id;
            }
            return null;
        }
    }




    #endregion




    #endregion

    #region Raise Event Methods

    public static void SendCacheToAll(string key, byte [] data)
    {
        Instance.m_connector.SendCachedDataToAll(key, data);
    }

    public static void SendCacheToAll(string key, LNSWriter writer)
    {
        Instance.m_connector.SendCachedDataToAll(key, writer);
    }

    public static void RaiseEventOnAll(ushort eventCode, LNSWriter writer, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
    {
        if(eventCode <= 10)
        {
            throw new System.Exception("Eventcode 0-10 is reserved for internal use");
        }
        Instance.m_connector.RaiseEventOnAll(eventCode, writer, deliveryMethod);
    }

    public static void RaiseEventOnClient(LNSClient client, ushort eventCode, LNSWriter writer, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
    {
        if (eventCode <= 10)
        {
            throw new System.Exception("Eventcode 0-10 is reserved for internal use");
        }
        Instance.m_connector.RaiseEventOnClient(client,eventCode, writer, deliveryMethod);
    }

    public static void RaiseEventOnMasterClient(ushort eventCode, LNSWriter writer, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
    {
        if (eventCode <= 10)
        {
            throw new System.Exception("Eventcode 0-10 is reserved for internal use");
        }
        Instance.m_connector.RaiseEventOnMasterClient(eventCode, writer, deliveryMethod);
    }

    #endregion


    
    private static LNSManager m_instance;

    private void Awake()
    {
        if(m_instance == null)
        {
            m_instance = this;
            
            GameObject.DontDestroyOnLoad(gameObject);
        }
        else if(m_instance != this)
        {
            Destroy(gameObject);
            
        }

    }

    private void OnDisable()
    {
        m_connector.Disconnect();
    }

    private void Update()
    {
        for(int i=0;i<netSyncTransmitters.Count;i++)
        {
            var currentTransmitter = netSyncTransmitters[i];
            if(currentTransmitter && currentTransmitter.enabled)
            {
                if(currentTransmitter.CanUpdate())
                {
                    currentTransmitter.ConsumeUpdate();
                    DATA_WRITER.Reset();
                    DATA_WRITER.Put(currentTransmitter.instanceId);
                    currentTransmitter.Write(DATA_WRITER);
                    m_connector.RaiseEventOnAll(0, DATA_WRITER, currentTransmitter.deliveryMethod);
                }   
            }
        }
    }


    #region Callbacks
    public void OnEventRaised(LNSClient from, ushort eventCode, LNSReader reader, DeliveryMethod deliveryMethod)
    {
        if(eventCode == 0)
        {
            //Internal Sync Methods
            string instanceId = reader.GetString();
            var netSyncReceiver = netSyncReceivers.Find(o => o.assignedClient.id == from.id && o.instanceId == instanceId);
            if(netSyncReceiver != null)
            {
                netSyncReceiver.assignedClient = from;
                netSyncReceiver.SetTimePackageReceived(Time.time);
                netSyncReceiver.ReadAndApply(reader);
            }
        }
        else
        {
            dataReceiver.OnEventRaised(from, eventCode, reader, deliveryMethod);
        }
    }

    public void OnCachedDataReceived(string ofKey, byte [] rawData)
    {
        dataReceiver.OnCachedDataReceived(ofKey,rawData);
    }
    #endregion
}
