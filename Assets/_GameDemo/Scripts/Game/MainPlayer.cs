using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainPlayer : MonoBehaviour
{
    // Start is called before the first frame update
    public float moveSpeed = 1;
    public float rotateSpeed = 10;
    

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKey(KeyCode.W))
        {
            transform.Translate(Vector3.forward * Time.deltaTime * moveSpeed);
        }
        if(Input.GetKey(KeyCode.A))
        {
            transform.Rotate(Vector3.up * -1 * rotateSpeed);
        }
        else if(Input.GetKey(KeyCode.D))
        {
            transform.Rotate(Vector3.up * rotateSpeed);
        }
    }
}
