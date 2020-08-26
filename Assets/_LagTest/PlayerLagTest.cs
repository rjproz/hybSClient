using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLagTest : MonoBehaviour
{
    public float ping = 200f;
    public float moveSpeed = 1;
    public float rotateSpeed = 10;

    private float milliseconds;

    private void Start()
    {
        milliseconds = ping / 1000f;
    }
    void Update()
    {
        Vector3 deltaPos = Vector3.zero;
        float deltaYAngle = 0;
        if (Input.GetKey(KeyCode.W))
        {
            deltaPos.z = Time.deltaTime * moveSpeed;
            
        }
        if (Input.GetKey(KeyCode.A))
        {
            deltaYAngle = -1 * rotateSpeed;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            deltaYAngle =  rotateSpeed;

        }

        StartCoroutine(Lag(deltaPos,deltaYAngle));
    }

    private IEnumerator Lag(Vector3 deltaPos, float deltaYAngle)
    {
        yield return new WaitForSeconds(milliseconds);

        transform.Translate(deltaPos);
        var angles = transform.rotation.eulerAngles;
        angles.y += deltaYAngle;
        transform.rotation = Quaternion.Euler(angles);
    }

}
