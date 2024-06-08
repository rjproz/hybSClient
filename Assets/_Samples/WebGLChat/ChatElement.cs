using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChatElement : MonoBehaviour
{
    public TMPro.TextMeshProUGUI nameTarget;
    public TMPro.TextMeshProUGUI msgTarget;
   

    public ChatElement Fill(string name,string msg)
    {
        ChatElement o = Instantiate(gameObject).GetComponent<ChatElement>();
        o.transform.SetParent(transform.parent);
        o.transform.localScale = transform.localScale;
        o.nameTarget.text = name;
        o.msgTarget.text = msg;
        o.gameObject.SetActive(true);
        return o;
    }
}
