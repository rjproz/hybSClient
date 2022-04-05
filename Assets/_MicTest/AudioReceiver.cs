using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.UI;

public class AudioReceiver : MonoBehaviour
{
    // Start is called before the first frame update
    public Text freqChannelsTarget;
    public AudioSource audioSource;
    public int playbackDelay = 1;
    AudioClip clip;
   
   


    Queue<float[]> pending = new Queue<float[]>();
    List<float> allPending = new List<float>();
   
   
    [SerializeField]
    private int freq;
    [SerializeField]
    private int channels;
    [SerializeField]
    private int pendingSamples;
    public void EnqueueData(int freq, int channels,float [] samples)// byte[] samplesCompressed, int dataLength)
    {

        //pending.Enqueue(samples);
        allPending.AddRange(samples);
        pendingSamples = allPending.Count;
        this.freq = freq;
        this.channels = channels;

        if (!enabled && allPending.Count >= freq * playbackDelay)
        //if (!enabled && pending.Count > 2)
        {
            freqChannelsTarget.text = string.Format("Freq: {0} Hz, Channels: {1}", freq, channels);
            enabled = true;
           
            //sampleLength = samples.Length;
        }
        
       
    }


    private void Update()
    {

        
        if (!audioSource.isPlaying && allPending.Count > 0)
        {
            var ac = AudioClip.Create("clip 2", allPending.Count, channels, freq, false);//, ReadCallback, SetPosition);//, pcmreadercallback: Callback);

            float[] samples = allPending.ToArray();
            FrostweepGames.VoicePro.EchoCancellation.Instance.RegisterFramePlayed(samples);
            ac.SetData(samples, 0);
            allPending.Clear();
            audioSource.clip = ac;
            audioSource.loop = false;
            audioSource.mute = false;

            audioSource.Play();

        }
        pendingSamples = allPending.Count;

        {


            //if (!audioSource.isPlaying)// || audioSource.timeSamples == sampleLength-1)
            //{
            //    //Debug.Log(audioSource.isPlaying);

            //    var data = pending.Dequeue();

            //    if (audioSource.clip == null)
            //    {
            //        clip = AudioClip.Create("clip3", data.Length, channels, freq, false);
            //        audioSource.clip = clip;
            //    }
            //    pendingSamples = pending.Count;
            //    //sampleLength = data.Length;
            //    audioSource.clip.SetData(data, 0);
            //    audioSource.loop = false;
            //    audioSource.mute = false;
            //    audioSource.Play();
            //}

        }
    }

    private void SetPosition(int position)
    {
        Debug.Log(position);
    }

    private void ReadCallback(float[] data)
    {
        
    }

    private void Start()
    {
        enabled = false;
    }


}
