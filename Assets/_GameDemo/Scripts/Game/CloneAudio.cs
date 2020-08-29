using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

public class CloneAudio : MonoBehaviour
{
    // Start is called before the first frame update
    public AudioSource audioSource;
    AudioClip clip;
    float[] samples;
    void Start()
    {
        samples = new float[1000000];
    }


    private int offset = 0;
    public void Play(int freq, int channels, byte[] samplesCompressed)
    {
        //Debug.Log("audioSource.isPlaying : "+audioSource.isPlaying);
        //while(audioSource.isPlaying)
        //{

        //}
        //Debug.Log(samplesCompressed.Length + " bytes");
        //Debug.Log("freq " + freq + " - channels: " + channels);
        samplesCompressed = Decompress(samplesCompressed);
        if (clip == null)
        {

            clip = AudioClip.Create("", samples.Length, channels, freq, false);
            audioSource.clip = clip;
        }

        for (int i = 0; i < samplesCompressed.Length; i++)
        {
            samples[i] = samplesCompressed[i] / 255f * 2f - 1;
        }

        clip.SetData(samples, offset);
        offset = offset + samplesCompressed.Length;

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
     

        if (offset >= samples.Length)
        {
            offset = 0;
            audioSource.Stop();
        }

    }

    void OnAudioRead(float[] data)
    {
        data = samples;
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
