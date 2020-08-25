using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseUI : MonoBehaviour
{
    private Canvas canvas;
    public Canvas GetCanvas()
    {
        if(canvas == null)
        {
            canvas = GetComponent<Canvas>();
        }
        return canvas;
    }
}
