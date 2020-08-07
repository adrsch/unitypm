using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PMSynth : MonoBehaviour
{
    public float amplitude;
    public int voices;
    private float sampleRate;
    public Oscillator[] Oscillators = new Oscillator[0];
    private bool[] voiceOn;
    private bool isReady;

    void Awake()
    {
        voiceOn = new bool[voices];
        sampleRate = AudioSettings.outputSampleRate;

        foreach (Oscillator osc in Oscillators)
        {
            osc.SetSampleRate(sampleRate);
            osc.SetVoices(voices);
        }
    }

    private int? useAvailableVoice()
    {
        for (int voice = 0; voice < voices; voice++)
        {
            if (voiceOn[voice]) { continue; }

            voiceOn[voice] = true;
            return (int?)voice;
        }
        return null;
    }


    // Returns voice used, or null if there isn't one available or if it hasn't been initialized yet. Velocity ranges from 0 to 1.
    public int? NoteOn(float frequency, float velocity)
    {
        int? voice = useAvailableVoice();
        if (voice == null) { return null; }
        
        foreach (Oscillator osc in Oscillators) 
        {
            osc.NoteOn(frequency, velocity, (int)voice);
        }
        return voice;
    }
    

    // Midi-style input. Midi note is converted to frequency: A4 is 69, at 440hz. Midi velocity is converted to a float from 0 to 1.
    public int? NoteOn(byte midiNote, byte midiVelocity) {
        return NoteOn(440f * Mathf.Pow(2f, ((midiNote - 69) / 12)), midiVelocity / 127);
    }

    public void NoteOff(int voice)
    {
        foreach (Oscillator osc in Oscillators)
        {
            osc.NoteOff(voice);
        }
    }
    
    void OnAudioFilterRead(float[] data, int channels)
    {
        for (int n = 0; n < data.Length / channels; n++)
        {
            //Sum up the outputs from all oscillators.
            float oscOutputs = 0;
            foreach (Oscillator osc in Oscillators)
            {
                for (int voice = 0; voice < voices; voice++)
                    if (voiceOn[voice] == true)
                    {
                        float? oscData = osc.GenerateData(voice);
                        if (oscData == null)
                            voiceOn[voice] = false;
                        else
                            oscOutputs += (float)oscData;
                    }
            }
            //Currently, no stereo support.
            for (int i = 0; i < channels; i++)
                data[n * channels + i] = amplitude * oscOutputs;

            n++;

        
        }
    }
}
