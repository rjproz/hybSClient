using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clone : BasePlayer
{
    public CloneAudio audioReceiver;
    public void SetColor(Color color)
    {
        GetComponent<Renderer>().material.color = color;
    }

    private Vector3 tpos = new Vector3(0,-1,0);
    private Quaternion trot;
    public void SetTarget(Vector3 pos,Quaternion rot)
    {
        tpos = pos;
        trot = rot;

        //transform.position = tpos;
        //transform.rotation = trot;
    }


    private void Update()
    {
        
        transform.position = Vector3.Lerp(transform.position, tpos, Time.deltaTime * 10f);
        transform.rotation = Quaternion.Lerp(transform.rotation, trot, Time.deltaTime * 5f);
    }

}
