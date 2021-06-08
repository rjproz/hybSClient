using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereGizmos : MonoBehaviour
{
    public Color color;
    public float radius = 10;
    public void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawSphere(transform.position, radius);
    }
}
