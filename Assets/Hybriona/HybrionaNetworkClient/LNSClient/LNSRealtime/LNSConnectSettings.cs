using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LNSConnectSettings 
{
    public string gameKey { get; set; }
    public string gameVersion { get; set; } = "1.0.0";
    public byte platform { get; private set; }
    public string serverSecurityKey { get; set; } = "rjproz";
    public string serverIp { get; set; } = "127.0.0.1";
    public int serverPort { get; set; } = 10002;

    public void Validate()
    {
        RuntimePlatform runtimePlatform = Application.platform;


        
        if(runtimePlatform == RuntimePlatform.WindowsPlayer )
        {
            platform = (byte)CLIENT_PLATFORM.DESKTOP_WINDOWS;
        }
        else if (runtimePlatform == RuntimePlatform.OSXPlayer)
        {
            platform = (byte)CLIENT_PLATFORM.DESKTOP_MACOS;
        }
        else if (runtimePlatform == RuntimePlatform.LinuxPlayer)
        {
            platform = (byte)CLIENT_PLATFORM.DESKTOP_LINUX;
        }
        else if (runtimePlatform == RuntimePlatform.Android)
        {
            platform = (byte)CLIENT_PLATFORM.ANDROID;
        }
        else if (runtimePlatform == RuntimePlatform.IPhonePlayer)
        {
            platform = (byte)CLIENT_PLATFORM.IOS;
        }
        else if (runtimePlatform == RuntimePlatform.WebGLPlayer)
        {
            platform = (byte)CLIENT_PLATFORM.WEBGL;
        }

#if UNITY_EDITOR
        platform = (byte)CLIENT_PLATFORM.UNITY_EDITOR;
#endif

        if (string.IsNullOrEmpty(gameKey))
        {
            gameKey = Application.identifier;
            Debug.Log(gameKey);
        }

        if (string.IsNullOrEmpty(gameVersion))
        {
            gameKey = Application.version;
        }
    }
}
