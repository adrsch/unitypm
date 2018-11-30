using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Oscillator : MonoBehaviour
{
    //Oscillator Properties
    private float[] frequency; //Set as needed by PMSynth component.
    private float[] velocity; //Set as needed by PMSynth component.
    private bool[] voiceOn;
    private int voices;

    private bool[] playing;

    public float ratio = 1;
    public float amplitude = 1; //Range is 0 to 1. Use 0 to -1 for inversion. Values beyond 1 will cause distortion.
    public float offset = 0; //Shifts the phase of the waves.
    public float feedback = 0; //Range is 0 to 1. Uses the previous calculation's data as if it were a standard modulator. This number functions as the modulation index would.

    //Modulators
    private float data = 0;
    private float sampleRate;
    private ulong[] time; //Will cause a click once this wraps around. Thankfully, this will not happen often, but it could be an issue for a continuously playing tone.
    public Oscillator[] modulators = new Oscillator[0]; //Please don't start an infinite loop of modulators. This will cause a stack overflow.
    public float[] modulationIndicies = new float[0]; //Optional. Acts as multiplier to the data of the corresponding modulator. Will default to 1.

    //Envelope
    public float attack = 0;
    public float decay = 0;
    public float sustain = 1;
    public float release = 0;
    
    private bool[] onRelease;
    private float[] offTime; //Used in calculating data during release
    private float[] offEnvelopeScalar; //Used in calculating data during release

    //Used for when osc a is modulated by osc b which is modulated by a. 
    private bool recursion = false;
        
    public void SetSampleRate(float sampleRate)
    {
        this.sampleRate = sampleRate;
        for (int i = 0; i < modulators.Length; i++)
            if (modulators[i] != null)
                modulators[i].sampleRate = sampleRate;
    }

    public void SetVoices(int voices)
    {
        if (recursion)
            return;
        recursion = true;
        frequency = new float[voices];
        velocity = new float[voices];
        voiceOn = new bool[voices];
        onRelease = new bool[voices];
        playing = new bool[voices];
        time = new ulong[voices];
        offTime = new float[voices];
        offEnvelopeScalar = new float[voices];
        this.voices = voices;
        for (int i = 0; i < modulators.Length; i++)
            if (modulators[i] != null)
                modulators[i].SetVoices(voices);
        recursion = false;
    }


    public float GenerateData(int voice)
    {
        if (playing[voice] == false)
            return float.NegativeInfinity; //This will set the synth to set the voice as inactive
        if (recursion)
            return data; //Feeding an oscillator's modulator the oscillator itself will result in a delay as is the case with feedback. 
        recursion = true;
        float phaseModulation = 0;
        for (int i = 0; i < modulators.Length; i++)
            if (modulators[i] != null)
                if (i < modulationIndicies.Length)
                    phaseModulation += modulationIndicies[i] * modulators[i].GenerateData(voice);
                else
                    phaseModulation += modulators[i].GenerateData(voice);
        data = GenerateEnvelopeScalar(voice) * velocity[voice] * amplitude * Mathf.Sin((float) (4 * Mathf.PI * ratio * frequency[voice] * time[voice]) / sampleRate + 1f * phaseModulation + feedback*data + offset); //Note that the feedback has a delay on it.
        time[voice]++;
        recursion = false;
        return data;
    }

    public float GenerateEnvelopeScalar(int voice)
    {
        float seconds = time[voice] / sampleRate;
        if (onRelease[voice] == false)
        {
            seconds = time[voice] / sampleRate;
            if ( seconds - attack < 0)
                return seconds / attack;
            if ((seconds - attack) - decay < 0)
                return 1 - (1- sustain) * (seconds - attack) / decay;
            if (sustain == 0)
                playing[voice] = false;
            return sustain;
        }
        if (seconds - offTime[voice] < release)
            return offEnvelopeScalar[voice] * (1 -  (seconds - offTime[voice]) / release);
        playing[voice] = false;
        return 0;
    }

    public int NoteOn(float frequency, byte velocity)
    {
        for (int i = 0; i < voices; i++)
            if (voiceOn[i] == false)
            {
                voiceOn[i] = true;
                this.frequency[i] = frequency;
                this.velocity[i] = velocity / 127;
                time[i] = 0;
                playing[i] = true;
                for (int j = 0; j < modulators.Length; j++)
                    if (modulators[j] != null)
                        modulators[j].NoteOn(frequency, 1, i);
                return (int)i;
            }
        return 256; //as this is larger than the highest possible voice, it is used as a failure to find a new voice
    }

    public void NoteOn(float frequency, float velocity, int voice)
    {
        if (recursion)
            return; 
        recursion = true;
        playing[voice] = true;
        time[voice] = 0;
        this.frequency[voice] = frequency;
        this.velocity[voice] = velocity;
        for (int i = 0; i < modulators.Length; i++)
            if (modulators[i] != null)
                modulators[i].NoteOn(frequency, velocity, voice);
        recursion = false;
    }

    public void NoteOff(int voice)
    {
        offTime[voice] = time[voice] / sampleRate;
        offEnvelopeScalar[voice] = GenerateEnvelopeScalar(voice);
        onRelease[voice] = true;
    }

}
