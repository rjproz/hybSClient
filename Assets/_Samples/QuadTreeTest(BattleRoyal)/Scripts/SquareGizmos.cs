using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquareGizmos : MonoBehaviour
{
    public Color color;
    
    public void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = color;

            Gizmos.DrawCube(transform.position, Vector3.one * ClientPopulator.Instance.searchExtends * 2);
        }
    }
}
