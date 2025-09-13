using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LNSMainThreadDispatcher : MonoBehaviour
{
    // Start is called before the first frame update
    private Queue<System.Action> actions = new Queue<System.Action>();
    protected static object thelock = new object();


    private static LNSMainThreadDispatcher instance;
    public static LNSMainThreadDispatcher GetInstance()
    {
        if(instance == null)
        {
            instance = GameObject.FindObjectOfType<LNSMainThreadDispatcher>();
            if(instance == null)
            {
                GameObject o = new GameObject("LNSMainThreadDispatcher");
                instance = o.AddComponent<LNSMainThreadDispatcher>();
                DontDestroyOnLoad(o);
            }
        }
        
        return instance;
    }

    public void Add(System.Action action)
    {
        lock (thelock)
        {
            actions.Enqueue(action);
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (actions.Count > 0)
        {
            lock (thelock)
            {
                while (actions.Count > 0)
                {
                    System.Action action = actions.Dequeue();
                    if(action != null)
                    {
                        try
                        {
                            action();
                        }
                        catch { }
                    }
                    
                }

            }
        }
    }

}
