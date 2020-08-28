using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

public class MainPlayerAudio : MonoBehaviour
{
    // Start is called before the first frame update


    public string deviceName = null;//"Blue Snowball";

    public int channels { get; private set; }
    public int frequency { get; private set; }
    public bool dataAvailable { get; private set; } = false;
    private byte[] rawData;
    int lastSample = 0;
    
    void Start()
    {
        //foreach(var d in Microphone.devices)
        // {
        //     Debug.Log(d);
        // }
        frequency = 20000;
        StartCoroutine(RecordCycle());

    }


    private IEnumerator RecordCycle()
    {


        AudioClip clip = Microphone.Start(deviceName, true, 100, frequency);
        channels = clip.channels;
        float[] samples = new float[1000000];
        while (true)
        {
            int pos = Microphone.GetPosition(deviceName);

            int diff = pos - lastSample;
            if (diff > 0)
            {


                int length = clip.channels * diff;
                byte[] samplesCompressed = new byte[length];
                clip.GetData(samples, lastSample);

                for (int i = 0; i < length; i++)
                {
                    samplesCompressed[i] = (byte)((int)((samples[i] + 1) * .5f * 255f));
                }

                rawData = Compress(samplesCompressed);
  
                dataAvailable = true;


            }
            lastSample = pos;
            yield return new WaitForSeconds(1f);
        }
    }

    public byte [] GetData()
    {
        if(dataAvailable)
        {
            dataAvailable = false;
            return rawData;
        }
        return null;
    }

    public static byte[] Compress(byte[] data)
    {
        MemoryStream output = new MemoryStream();
        using (DeflateStream dstream = new DeflateStream(output, System.IO.Compression.CompressionLevel.Fastest))
        {
            dstream.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }
}
