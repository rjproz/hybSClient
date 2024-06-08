using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CCUTestSimple : MonoBehaviour
{
    
    public int ccu = 100;
    public string url;
    public Text successText;
    public Text errorText;
    public int success = 0;
    void Start()
    {
        success = 0;
        for(int i=0;i<ccu;i++)
        {
            StartCoroutine(Hit());
        }
    }

    // Update is called once per frame
    IEnumerator Hit()
    {
        WWW www = new WWW(url);
        yield return www;
        try
        {
           
            if (string.IsNullOrEmpty(www.error))
            {
                success++;
                successText.text = success.ToString();
            }
            else
            {
                errorText.text += "\nWWW: " + www.error;
            }
        }
        catch(System.Exception ex)
        {
            errorText.text += "\n" + ex.Message;
        }
        yield return null;
    }
}
