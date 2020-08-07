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
    private bool voicesReady;
    private bool audioReady;

    void Start()
    {
        if (!voicesReady) { setupVoices(); }
        if (!audioReady) { setupAudio(); }
    }

    private void setupVoices()
    {
        voiceOn = new bool[voices];
        foreach (Oscillator osc in Oscillators)
        {
            osc.SetVoices(voices);
        }
        voicesReady = true;
    }
    
    // Must be run in main thread.
    private void setupAudio()
    {
        sampleRate = AudioSettings.outputSampleRate;
        foreach (Oscillator osc in Oscillators)
        {
            osc.SetSampleRate(sampleRate);
        }
        audioReady = true;
    }


    private int? useAvailableVoice()
    {
        if (!voicesReady) { setupVoices(); }
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
        if (!voicesReady) { setupVoices(); }
        foreach (Oscillator osc in Oscillators)
        {
            osc.NoteOff(voice);
        }
    }
    
    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!audioReady) { return; } // Cannot fetch sample rate from this thread, wait for loading instead.

        for (int n = 0; n < data.Length / channels; n += channels)
        {
            // Sum up the outputs from all oscillators.
            float oscOutputs = 0;
            foreach (Oscillator osc in Oscillators)
            {
                for (int voice = 0; voice < voices; voice++)
                {
                    if (voiceOn[voice] != true) { continue; }
                    float? oscData = osc.GenerateData(voice);
                    if (oscData == null)
                    {
                        voiceOn[voice] = false;
                    }
                    else
                    {
                       oscOutputs += (float)oscData;
                    }
                }
            }
            // Currently, no stereo support - set the output in all channels to the summed osc outputs.
            for (int channel = 0; channel < channels; channel++)
            {
                data[n * channels + channel] = amplitude * oscOutputs;
            }
        }
    }
}
