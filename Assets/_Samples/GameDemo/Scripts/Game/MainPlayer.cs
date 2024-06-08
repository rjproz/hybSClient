using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainPlayer : BasePlayer
{
    // Start is called before the first frame update
    
    public float moveSpeed = 1;
    public float rotateSpeed = 10;
   
    public Color color;
    private Rigidbody rigidbody;
    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        transform.position = new Vector3(Random.Range(-50, 50), .5f, Random.Range(-50, 50));
        color.r = Random.value;
        color.g = Random.value;
        color.b = Random.value;
        transform.GetComponent<Renderer>().material.color = color;
    }
    // Update is called once per frame
    void Update()
    {
        if(Input.GetKey(KeyCode.W))
        {
            transform.Translate(Vector3.forward * Time.deltaTime * moveSpeed);
        }
        else if (Input.GetKey(KeyCode.S))
        {
            transform.Translate(Vector3.back * Time.deltaTime * moveSpeed);
        }
        if (Input.GetKey(KeyCode.A))
        {
            transform.Rotate(Vector3.up * -1 * rotateSpeed);
        }
        else if(Input.GetKey(KeyCode.D))
        {
            transform.Rotate(Vector3.up * rotateSpeed);
        }

        if(Input.GetKeyUp(KeyCode.Space))
        {
            rigidbody.AddForce(Vector3.up * 500);
        }

        Globals.Instance.multiplayerClient.SendTransform(transform.position, transform.rotation);
       


        if (Input.GetMouseButtonUp(0))
        {
            Vector3 from = transform.position + transform.forward * .7f;
            Quaternion rot = transform.rotation;
            ShootBulletAt(from, rot);
            Globals.Instance.multiplayerClient.SendBulletInvoke(from, rot);
        }
    }

    public void TakeDamage(Vector3 direction)
    {
        rigidbody.AddForce(direction * 500);
    }
}
