using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasePlayer : MonoBehaviour
{
    public GameObject bulletPrefab;
   

    public void ShootBulletAt(Vector3 pos,Quaternion rot)
    {
        GameObject o = GameObject.Instantiate(bulletPrefab);
        o.GetComponent<Bullet>().AppearAt(pos, rot);
    }
}
