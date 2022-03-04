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
    public int diff;
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
        byte[] samplesByte = new byte[10000000];
        while (true)
        {
            int pos = Microphone.GetPosition(deviceName);
            
            diff = pos - lastSample;
            if (diff > 0)
            {


                int length = clip.channels * diff;
               
                clip.GetData(samples, lastSample);
                //Debug.LogFormat("samplesCompressed: {0} , samples: {1}, currentLength: {2}", samplesByte.Length,samples.Length,length);
                for (int i = 0; i < length; i++)
                {
                    samplesByte[i] = (byte)((int)((samples[i] + 1) * .5f * 255f));
                }

                //for (int i = 0; i < samples.Length; i++)
                //{


                //    if (samples[i] < noiseGate)
                //    {
                //        samples[i] = 0;
                //    }
                //}
                //Debug.Log("Before: " + length);
                //byte [] compressed = Compress(samplesByte, length);
                //Debug.Log("After: " + compressed.Length);

                receiver.Send(freq,clip.channels, samplesByte);
            }
            lastSample = pos;
            yield return null;
        }
    }
    public static byte[] Compress(byte[] data, int length)
    {
        MemoryStream output = new MemoryStream();
        using (DeflateStream dstream = new DeflateStream(output, System.IO.Compression.CompressionLevel.Fastest))
        {
            dstream.Write(data, 0, length);
        }
        return output.ToArray();
    }
}
