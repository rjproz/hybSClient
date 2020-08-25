using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clone : MonoBehaviour
{
    public void SetColor(Color color)
    {
        GetComponent<Renderer>().material.color = color;
    }

    private Vector3 tpos;
    private Quaternion trot;
    public void SetTarget(Vector3 pos,Quaternion rot)
    {
        tpos = pos;
        trot = rot;
    }


    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, tpos, Time.deltaTime * 10f);
        transform.rotation = Quaternion.Lerp(transform.rotation, trot, Time.deltaTime * 5f);
    }

}
