using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using UnityEngine;

public class AudioRecorder : MonoBehaviour
{
    // Start is called before the first frame update
    public AudioClip clip;
   
    public string deviceName = "Blue Snowball";
    [Range(-1, 1)]
    public float noiseGate = -1;
    public AudioReceiver receiver;

    int lastSample = 0;
    void Start()
    {
        //foreach(var d in Microphone.devices)
        // {
        //     Debug.Log(d);
        // }
        StartCoroutine(RecordCycle());
        
    }

    private IEnumerator RecordCycle()
    {
        int freq = 50000;
        
        clip = Microphone.Start(deviceName, true, 100, freq);
        
        float[] samples = new float[10000000];
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

                //for (int i = 0; i < samples.Length; i++)
                //{


                //    if (samples[i] < noiseGate)
                //    {
                //        samples[i] = 0;
                //    }
                //}
                //Debug.Log("Before: " + samplesCompressed.Length);
                samplesCompressed = Compress(samplesCompressed);
                //Debug.Log("After: " + samplesCompressed.Length);

                receiver.Send(freq,clip.channels, samplesCompressed);
            }
            lastSample = pos;
            yield return new WaitForSeconds(1f);
        }
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
