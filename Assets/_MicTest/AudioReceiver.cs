using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

public class AudioReceiver : MonoBehaviour
{
    // Start is called before the first frame update
    public AudioSource audioSource;
    AudioClip clip;
    float[] samples;
    void Start()
    {
        samples = new float[100000];
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    internal void Send(int freq,int channels,byte[] samplesCompressed)
    {
        Debug.Log(samplesCompressed.Length + " bytes");
        samplesCompressed = Decompress(samplesCompressed);
        if (clip == null)
        {

            clip = AudioClip.Create("", samples.Length, channels, freq, false);
        }

        for(int i=0;i<samplesCompressed.Length;i++)
        {
            samples[i] = samplesCompressed[i] / 255f * 2f - 1; 
        }

        clip.SetData(samples, 0);
        audioSource.clip = clip;

        //float max = Mathf.NegativeInfinity;
        //for(int i=0;i<samples.Length;i++)
        //{
        //    if(samples[i] > max)
        //    {
        //        max = samples[i];
        //    }
        //}

        //Debug.Log("MAX " + max);
        audioSource.Stop();
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }

    }

    public static byte[] Decompress(byte[] data)
    {
        byte[] arr = null;
        using (MemoryStream input = new MemoryStream(data))
        {
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }
            arr = output.ToArray();
        }
        return arr;
    }
}
