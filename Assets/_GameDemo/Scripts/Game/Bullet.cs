using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public bool isDummy;
    public void AppearAt(Vector3 pos,Quaternion rot)
    {
        gameObject.SetActive(true);
        transform.position = pos;
        transform.rotation = rot;
       
        StartCoroutine(Process());
    }

    IEnumerator Process()
    {
        float timeStarted = Time.time;
        while(Time.time - timeStarted < 2)
        {
            transform.Translate(Vector3.forward * Time.deltaTime * 100);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!isDummy)
        {
            if (other.CompareTag("Player"))
            {
                other.GetComponent<MainPlayer>().TakeDamage(transform.forward);
            }
        }
        Destroy(gameObject);
    }
}
