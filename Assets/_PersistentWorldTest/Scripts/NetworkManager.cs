using System.Collections;
using System.Collections.Generic;
using LiteNetLib;
using UnityEngine;

public class NetworkManager : MonoBehaviour,ILNSManagerDataReceiver
{
    public bool isServer { get; set; } = false;

    public GameObject treePrefab;
    public Transform plantHud;
    private Dictionary<string, TreeNetwork> trees = new Dictionary<string, TreeNetwork>();

   
    public void Awake()
    {
        treePrefab.SetActive(false);
        Application.targetFrameRate = 60;
        //StartCoroutine(TestLerp());
#if UNITY_SERVER && !UNITY_EDITOR
        isServer = true;
        Application.targetFrameRate = 10;
#endif
    }

    public float to = 30;
    public float current;
    public float speed = .1f;
    public float timeCompleted;
    IEnumerator TestLerp()
    {
        float timeStarted = Time.realtimeSinceStartup;
        while(true)
        {
            yield return null;

            current = current + speed * Time.deltaTime;
            if(current > to)
            {
                timeCompleted = Time.realtimeSinceStartup - timeStarted;
                yield break;
            }
        }
    }

     
    [ContextMenu("Plant At Hud")]
    public void PlantAtHUD()
    {
        PlantTreeAt(plantHud.position);
        plantHud.position += Vector3.forward;
    }

    public void PlantTreeAt(Vector3 pos)
    {

        

        LNSWriter writer = LNSWriter.GetFromPool();
        writer.Reset();
        writer.Put(pos);
        
        LNSManager.RaiseEventOnMasterClient(12, writer, DeliveryMethod.ReliableOrdered);
        writer.Recycle();
    }


    void ILNSManagerDataReceiver.OnCachedDataReceived(string ofKey, byte[] rawData)
    {
        //throw new System.NotImplementedException();
    }

    void ILNSManagerDataReceiver.OnEventRaised(LNSClient from, ushort eventCode, LNSReader reader, DeliveryMethod deliveryMethod)
    {
        //throw new System.NotImplementedException();
        
        if(eventCode == 12)
        {
            if (isServer)
            {
                //plant
                
                var pos = reader.GetVector3();

                var tree = Instantiate(treePrefab);
                tree.transform.position = pos;
                tree.SetActive(true);

                var treeScript = tree.GetComponent<TreeNetwork>();
                treeScript.id = LNSManager.GenerateNextId();
                treeScript.StartGrow();


                trees.Add(treeScript.id, treeScript);
            }

        }
        else if (eventCode == 13)
        {
            if (isServer)
            {
                //delete
                var id = reader.GetString();
                var ga = trees[id];
                trees.Remove(id);
                Destroy(ga.gameObject);
                Resources.UnloadUnusedAssets();
            }

        }
        else if(eventCode == 11)
        {
            
            var id = reader.GetString();
            var pos = reader.GetVector3();
            var scale = reader.GetVector3();

            TreeNetwork treeNetwork = null;
            if(trees.ContainsKey(id))
            {
                treeNetwork = trees[id];
            }
            else
            {
               
                var tree = Instantiate(treePrefab);
                tree.transform.position = pos;
                tree.SetActive(true);

                var treeScript = tree.GetComponent<TreeNetwork>();
                treeScript.id = id;
                trees.Add(treeScript.id, treeScript);
                treeNetwork = treeScript;

            }
            treeNetwork.transform.position = pos;
            treeNetwork.transform.localScale = scale;
        }
    }



    // Start is called before the first frame update
    void Start()
    {
        LNSClientParameters clientParameters = new LNSClientParameters(SystemInfo.deviceUniqueIdentifier + Application.isEditor);
        LNSConnectSettings connectSettings = new LNSConnectSettings();
       
        connectSettings.serverIp = "45.55.33.88";
        //connectSettings.serverIp = "localhost";
        connectSettings.serverPort = 10002;
        connectSettings.serverSecurityKey = ServerKey.GetKey();
        connectSettings.gameKey = "hybriona.persistent_world";

        LNSManager.Initialize(clientParameters, connectSettings, this);


        System.Action roomAction = () =>
        {
            Debug.Log("Room joined");
            if (isServer)
            {
                Debug.Log("I am server");
                LNSManager.connector.MakeMeMasterClient();
               
            }
        };
        LNSManager.connector.onRoomCreated = ()=> roomAction();
        LNSManager.connector.onRoomJoined = ()=> roomAction();
        LNSManager.connector.onConnected = () =>
        {
            Debug.Log("Connected");

            //LNSManager.connector.JoinRoomOrCreateIfNotExist("default");
            LNSManager.connector.QueryIfRoomExists("default");
        };


        LNSManager.connector.onRoomExistsResponse = (roomId, exists) =>
        {
            Debug.Log(roomId + " Room Exists? " + exists);
            if(exists)
            {
                LNSManager.connector.JoinRoom("default");
            }
            else
            {
                LNSCreateRoomParameters roomParameters = new LNSCreateRoomParameters();
                roomParameters.idleLife = 1;
                LNSManager.connector.CreateRoom("default", roomParameters);
            }
        };

        LNSManager.Connect();
    }


    private void Update()
    {
        if(!isServer)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hitInfo;
                if (Physics.Raycast(ray, out hitInfo))
                {
                    if (hitInfo.collider.gameObject.name == "Plane")
                    {
                        PlantTreeAt(hitInfo.point);
                    }
                }
            }
        }
    }

}
