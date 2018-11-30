using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PMSynth : MonoBehaviour 
{
    public float amplitude;
    public byte voices;
    private float sampleRate;
    public Oscillator[] Oscillators = new Oscillator[0];
    private bool[] voiceOn;
    private bool on;

    void Start()
    {
        voiceOn = new bool[voices];
        sampleRate = AudioSettings.outputSampleRate;
        for (int i = 0; i < Oscillators.Length; i++)
        {
            Oscillators[i].SetSampleRate(sampleRate);
            Oscillators[i].SetVoices(voices);
        }
        on = true;
    }

    public bool IsOn()
    {
        return on;
    }

    //Returns voice used, or null if there isn't one available or if it hasn't been initialized yet. Uses midi velocity, ranging from 0 to 127. 
    public int? NoteOn(float frequency, byte velocity)
    {
        if (!on)
            return null;
        int? i;
        for (i = 0; i < voices; i++)
            if (voiceOn[(int)i] == false)
                for (int j = 0; j < Oscillators.Length; j++)
                {
                    Oscillators[j].NoteOn(frequency, velocity / 127, (int)i);
                    voiceOn[(int)i] = true;
                }
        return i;
    }
    

   public void NoteOff(int voice)
    {
        for (int i = 0; i < Oscillators.Length; i++)
            Oscillators[i].NoteOff(voice);
    }
    
    void OnAudioFilterRead(float[] data, int channels)
    {
        if (on)
            for (int n = 0; n < data.Length / channels; n++)
            {
                //Sum up the outputs from all oscillators.
                float oscOutputs = 0;
                for (int j = 0; j < Oscillators.Length; j++)
                    for (int i = 0; i < voices; i++)
                        if (voiceOn[i] == true)
                        {
                            float oscData = Oscillators[j].GenerateData(i);
                            if (float.IsNegativeInfinity(oscData))
                                voiceOn[i] = false;
                            else
                                oscOutputs += oscData;
                        }
                //Currently, no stereo support.
                for (int i = 0; i < channels; i++)
                    data[n * channels + i] = amplitude * oscOutputs;

                n++;

            
            }
    }
}