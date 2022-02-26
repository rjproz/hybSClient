using System;
using UnityEngine;
public class ServerKey
{
    private static string mKey;
    public static string GetKey()
    {
        if (string.IsNullOrEmpty(mKey))
        {
            mKey = Resources.Load<TextAsset>("DontSync/serverkey").text;
        }
        return mKey;
    }
}
