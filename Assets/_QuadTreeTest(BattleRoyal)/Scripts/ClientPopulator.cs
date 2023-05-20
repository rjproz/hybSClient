using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientPopulator : MonoBehaviour
{
    public GameObject gameSessionPrefab;
    public float searchExtends = 200;
    public int mapSize = 2000;
    public int totalClient = 50;
    int x = 1;
    int z = 0;

    public static ClientPopulator Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
    IEnumerator Start()
    {
        Application.targetFrameRate = 60;
        for(int i=0;i<totalClient;i++)
        {
            GameObject newSession = Instantiate(gameSessionPrefab, gameSessionPrefab.transform.parent, true);
            GameSession script = newSession.GetComponent<GameSession>();
            script.transform.Find("Ground").localScale = Vector3.one * mapSize * .1f;
            script.mapSize = mapSize;
            script.StartProcess((i+1).ToString());
            newSession.name = "Session " + (i + 1).ToString();
            newSession.transform.position = new Vector3(x, 0, z) * mapSize * 1.1f;
            if(x == 10)
            {
                x = -10;
                z++;
            }
            else
            {
                x++;
            }
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
        }

        Destroy(gameSessionPrefab);
    }

    
}
