using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPSText : MonoBehaviour
{
    // Start is called before the first frame update
    public Text text;

    private void Update()
    {
        text.text = (1f / Time.deltaTime).ToString("0.00");
    }


}
