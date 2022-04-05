using System.IO;
using System.IO.Compression;
using System.Threading;
using LiteNetLib;
using UnityEngine;

public class Session : MonoBehaviour, ILNSDataReceiver
{
    public bool localEchoTest = true;
    public bool useLocalServer = true;
    public AudioRecorder audioRecorder;
    public AudioReceiver audioReceiver;
    private LNSConnector connector;

    static object writeLock = new object();
    LNSWriter netWriter;

   
   
    public void OnCachedDataReceived(string key, byte [] rawData)
    {
        
    }


    byte[] buffer;
    float[] floatBuffer;
    private void OnDataAvailable(int freq, int channel, float[] floats)
    {
        if(localEchoTest)
        {
            if (floats != null && floats.Length > 0)
            {
                audioReceiver.EnqueueData(FrostweepGames.VoicePro.Constants.SampleRate, FrostweepGames.VoicePro.Constants.Channels, floats);
            }
            return;
        }
        new Thread(() =>
        {
            if (connector != null && connector.isConnected && connector.isInActiveRoom)
            {
                lock (writeLock)
                {
                    //if(floats.Length  * 2 > 1000)
                    //{
                    //    //need to devide
                    //}

                    netWriter.Reset();
                    netWriter.Put(freq);
                    netWriter.Put(channel);

                    netWriter.Put(floats.Length);
                    for (int i=0;i<floats.Length;i++)
                    {
                        netWriter.Put((short)(floats[i] * 32767));
                    }
                    
                  

                   
                    connector.RaiseEventOnAll(1, netWriter, DeliveryMethod.ReliableOrdered);
                }

            }
        }).Start();
    }

    public void OnEventRaised(LNSClient from, ushort eventCode, LNSReader reader, DeliveryMethod deliveryMethod)
    {
//#if !UNITY_EDITOR
//        return;
//#endif
        if (eventCode == 1)
        {
            int freq = reader.GetInt();
            int channel = reader.GetInt();

            int sampleSize = reader.GetInt();
            floatBuffer = new float[sampleSize];
            for (int i=0;i<sampleSize;i++)
            {
                floatBuffer[i] = reader.GetShort() / 32767f;
            }
            
            audioReceiver.EnqueueData(freq, channel, floatBuffer);


            //if (mode == 0)
            //{
            //    byte[] data = reader.GetRemainingBytes();

            //    audioReceiver.Play(freq, channel, data, data.Length);
            //}
            //else if (mode == 1)
            //{
            //    float[] floats = reader.GetFloatArray();
            //    //byte[] data = reader.GetRemainingBytes();

            //    audioReceiver.Play(freq, channel, floats);
            //}
        }
    }

    private void Start()
    {
        audioRecorder.onDataAvailable = OnDataAvailable;
        if (localEchoTest)
        {
            return;
        }
        var config = AudioSettings.GetConfiguration();
        config.dspBufferSize = 1024;
        Debug.Log("dspBufferSize: " + config.dspBufferSize);
        //AudioSettings.Reset(config);

        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        LNSClientParameters clientParameters = new LNSClientParameters(SystemInfo.deviceUniqueIdentifier + Application.isEditor + System.DateTime.Now.Second, gameObject.name);
        LNSConnectSettings connectSettings = new LNSConnectSettings();
        connectSettings.serverIp = "45.55.33.88";

        connectSettings.serverPort = 10002;
        connectSettings.serverSecurityKey = ServerKey.GetKey();

        if(useLocalServer)
        {
            connectSettings.serverIp = "192.168.0.100";
            connectSettings.serverSecurityKey = "demokey";
        }
        connectSettings.gameKey = "hybriona.voicechat";
       
        connector = new LNSConnector(clientParameters, connectSettings, this);

        connector.onConnected = () =>
        {
            LNSCreateRoomParameters roomParameters = new LNSCreateRoomParameters();
            roomParameters.maxPlayers = 1000;
            roomParameters.isQuadTreeAllowed = false;
            //roomParameters.idleLife = 60 * 24;
            //roomParameters.EnableQuadTreeCellOptimization(Vector2.zero, new Vector2(2000, 2000));

            
            connector.CreateRoom("default", roomParameters);
        };

        connector.onFailedToConnect = (CONNECTION_FAILURE_CODE code) =>
        {
            Debug.LogError(name + " - " + code);
        };

        connector.onDisconnectedFromRoom = () =>
        {
            Debug.LogError(name + " - onDisconnectedFromRoom");
        };

        connector.onDisconnected = () =>
        {
            Debug.LogError(name + " - onDisconnectedFromServer");
        };

        connector.onRoomCreateFailed = (ROOM_FAILURE_CODE code) =>
        {
            Debug.LogError(name + " - " + code);
            if (code == ROOM_FAILURE_CODE.ROOM_ALREADY_EXIST)
            {
                connector.JoinRoom("default");
            }
        };

        connector.onRoomJoined = () =>
        {
            Debug.Log(name + " on room joined");
        };
        connector.onRoomJoinFailed = (ROOM_FAILURE_CODE code) =>
        {
            Debug.LogError(name + " - " + code);
        };
        connector.Connect();

        netWriter = new LNSWriter();
       
    }

   
    

    private void OnDisable()
    {
        connector.Disconnect();
    }

    public static byte[] Compress(byte[] data, int length)
    {
        MemoryStream output = new MemoryStream();
        using (DeflateStream dstream = new DeflateStream(output, System.IO.Compression.CompressionLevel.Fastest))
        {
            dstream.Write(data, 0, length);
        }
        return output.ToArray();
    }
}
