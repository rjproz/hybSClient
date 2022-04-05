using System.Collections.Generic;
using UnityEngine;
using Adrenak.UniMic;
using UnityEngine.UI;
using FrostweepGames.VoicePro;
public class AudioRecorder : MonoBehaviour
{
    // Start is called before the first frame update
    public bool allowAEC;
    public Dropdown micSelectionDropDown;
    public Text micNameTarget;
    
    private AudioClip clip;
    private AudioSource assignedSource;
   
    public System.Action<int,int,float []> onDataAvailable;
    int lastSample = 0;

    [SerializeField]
    int freq = 16000;
    [SerializeField]
    int sampleMS = 200;
    [SerializeField]
    float detectionThreshold = 0.01f;

    
    [SerializeField]
    private Vector2 min_max = new Vector2(10,-10);


   
   

    [SerializeField]
    bool allowFilter;
    [SerializeField]
    float recentValue;

  /*
    private void OnAudioFilterRead(float[] data, int channels)
    {


        if (!allowFilter)
        {
            return;
        }
       
        {
           
            for (int i = 0; i < data.Length; i++)
            {


     
              
                if (data[i] < min_max.x)
                {
                    min_max.x = data[i];
                }
                else if (data[i] > min_max.y)
                {
                    min_max.y = data[i];
                }
             
            }

            //min_max.x = min;
            //min_max.y = max;
            for (int i = 0; i < data.Length; i++)
            {
                float val = data[i] ;
                val = (val - min_max.x) / (min_max.y - min_max.x);
                val = val * 2 - 1;
                data[i] = val * sampleMultiplier / 100f;
               
            }
           
        }
    }
  */
    private EchoCancellation echoCancellation;
    void Start()
    {
        Constants.SampleRate = freq;
        Constants.ChunkTime = sampleMS;

        assignedSource = GetComponent<AudioSource>();
        Mic.Instantiate();
        var mic = Mic.Instance;
        
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();

        for(int i=0;i<mic.Devices.Count;i++)
        {
            options.Add(new Dropdown.OptionData(mic.Devices[i]));
        }
        micSelectionDropDown.options = options;
        if(PlayerPrefs.HasKey("mic"))
        {
            mic.SelectDevice(PlayerPrefs.GetString("mic"));
            micSelectionDropDown.SetValueWithoutNotify(mic.CurrentDeviceIndex);
        }

        //mic.ChangeDevice(-1);
        int minFreq = 0;
        int maxFreq = 0;
        Microphone.GetDeviceCaps(mic.CurrentDeviceName, out minFreq, out maxFreq);
        Debug.LogFormat("GetDeviceCaps Max Freq: {0}, Min Freq: {1}", maxFreq,minFreq);

       ;
        mic.StartRecording(freq, sampleMS);
        micNameTarget.text = "Mic :"+mic.CurrentDeviceName;
        bool first = true;


        List<float> queueSamples = new List<float>();
        float[] outputSamples = new float[freq * sampleMS / 1000];

        bool voiceDetectedLastTime = false;
        float voiceDetectionTime = 0;
        mic.OnSampleReady += (int index, float [] samples) =>
        {
            
            voiceDetectedLastTime = false;
            for (int i=0;i<samples.Length;i++)
            {
                if(Mathf.Abs(samples[i]) > detectionThreshold)
                {
                    voiceDetectedLastTime = true;
                    voiceDetectionTime = Time.time;
                    break;
                }
            }

            
            if(!voiceDetectedLastTime && Time.time - voiceDetectionTime > 1)
            {
                assignedSource.volume = 1;
                return;
            }
            assignedSource.volume = .4f;

            if (allowAEC || Input.touchCount == 2)
            {
                
                EchoCancellation.Instance.RegisterFrameRecorded(samples);

                EchoCancellation.Instance.GetProcessEchoCancellationFrame((float[] output) =>
                {
                    onDataAvailable(freq, Mic.Instance.AudioClip.channels, output);
                });
            }
            else
            {
                onDataAvailable(freq, Mic.Instance.AudioClip.channels, samples);
            }
            //EchoCancellation.Instance.re
           




            /*
             queueSamples.AddRange(samples);

            if (index == 10000/ sampleMS)
            {
                var rawSamples = queueSamples.ToArray();
                var newClip = AudioClip.Create("gen", queueSamples.Count, mic.AudioClip.channels, freq, false);
                newClip.SetData(rawSamples, 0);

                assignedSource.clip = newClip;
                assignedSource.Play();
                assignedSource.volume = 1;
                first = false;
 #if !UNITY_EDITOR

 #endif

                Debug.Log("rawSamples Count: " + rawSamples.Length);
                //onDataAvailable(newClip.frequency, mic.AudioClip.channels, rawSamples);
            }
            */

        };

        micSelectionDropDown.onValueChanged.AddListener((int selected) =>
        {
            Debug.Log("On Dropdown changed");
            mic.ChangeDevice(selected);
            micNameTarget.text = "Mic :" + mic.CurrentDeviceName;
            PlayerPrefs.SetString("mic", mic.CurrentDeviceName);
            PlayerPrefs.Save();
        });
    }

    //private void Update()
    //{
    //    if (!assignedSource.isPlaying && Mic.Instance.GetPosition() > 0)
    //    {
    //        assignedSource.clip = Mic.Instance.AudioClip;
    //        assignedSource.Play();
    //    }
    //}
    //private IEnumerator RecordCycle()
    //{


    //    clip = Microphone.Start("", true, 100, freq);

    //    float[] samples = null;// new float[10000000];
    //                           // byte[] samplesByte = new byte[10000000];
    //                           //float[] samplesFloat = null;
    //    Debug.Log("ClipChannels: " + clip.channels);

    //    while (true)
    //    {
    //        int pos = Microphone.GetPosition("");

    //        int diff = pos - lastSample;
    //        if (diff > 0)
    //        {


    //            int length = clip.channels * diff;

    //            samples = new float[length];
    //            clip.GetData(samples, lastSample);

    //            if (onDataAvailable != null)
    //            {
    //                //onDataAvailable(freq, clip.channels, samples);
    //                var clip = AudioClip.Create("clip", samples.Length, 1, freq, false);
    //                clip.SetData(samples, 0);
    //                audioSource.clip = clip;
    //                audioSource.loop = false;
    //                audioSource.mute = false;
    //                audioSource.Play();
    //            }

    //        }
    //        lastSample = pos;
    //        yield return  new WaitForSeconds(1f);
    //    }
    //}

}
