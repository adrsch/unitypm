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

    private float[] data;
    private float sampleRate;
    private ulong[] time; //Will cause a click once this wraps around. Thankfully, this will not happen often, but it could be an issue for a continuously playing tone.

    //Modulators
    public Oscillator[] modulators = new Oscillator[0]; //Please don't start an infinite loop of modulators. This will cause a stack overflow.
    public float[] modulationIndicies = new float[0]; //Optional. Acts as multiplier to the data of the corresponding modulator. Will default to 1.

    //Envelope
    public float attack = 0;
    public float decay = 0;
    public float sustain = 1;
    public float release = 0;

    public LFO[] ratioLFO = new LFO[0];
    public float[] ratioLFOModulation = new float[0];
    public LFO[] amplitudeLFO = new LFO[0];
    public float[] amplitudeLFOModulation = new float[0];
    public LFO[] feedbackLFO = new LFO[0];
    public float[] feedbackLFOModulation = new float[0];
    public LFO[] indiciesLFO = new LFO[0];
    public float[] indiciesLFOModulation = new float[0];

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
        for (int i = 0; i < ratioLFO.Length; i++)
            if (ratioLFO[i] != null)
                ratioLFO[i].SetSampleRate(sampleRate);
        for (int i = 0; i < amplitudeLFO.Length; i++)
            if (amplitudeLFO != null)
                amplitudeLFO[i].SetSampleRate(sampleRate);
        for (int i = 0; i < feedbackLFO.Length; i++)
            if (feedbackLFO != null)
                feedbackLFO[i].SetSampleRate(sampleRate);
        for (int i = 0; i < indiciesLFO.Length; i++)
            if (indiciesLFO[i] != null)
                indiciesLFO[i].SetSampleRate(sampleRate);
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
        data = new float[voices];
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
            return data[voice]; //Feeding an oscillator's modulator the oscillator itself will result in a delay as is the case with feedback. 
        recursion = true;
        
        //LFOs (except modulation indicies LFO - that is done in phase modulation)
        float modulatedRatio = ratio;
        for (int i = 0; i < ratioLFO.Length; i++)
            if (ratioLFO[i] != null)
                if (i < ratioLFOModulation.Length)
                    modulatedRatio = ratioLFOModulation[i] * (ratioLFO[i].generateData(time[voice]) - modulatedRatio) + modulatedRatio;
                else
                    modulatedRatio *= ratioLFO[i].generateData(time[voice]);
        float modulatedAmplitude = amplitude;
        for (int i = 0; i < amplitudeLFO.Length; i++)
            if (amplitudeLFO[i] != null)
                if (i < amplitudeLFOModulation.Length)
                    modulatedAmplitude = amplitudeLFOModulation[i] * (amplitudeLFO[i].generateData(time[voice]) - modulatedAmplitude) + modulatedAmplitude;
                else
                    modulatedAmplitude *= amplitudeLFO[i].generateData(time[voice]);
        float modulatedFeedback = feedback;
        for (int i = 0; i < feedbackLFO.Length; i++)
            if (feedbackLFO[i] != null)
                if (i < feedbackLFOModulation.Length)
                    modulatedFeedback = feedbackLFOModulation[i] * (feedbackLFO[i].generateData(time[voice]) - modulatedFeedback) + modulatedFeedback;
                else
                    modulatedFeedback *= feedbackLFO[i].generateData(time[voice]);

        //Phase Modulation
        float phaseModulation = 0;
        for (int i = 0; i < modulators.Length; i++)
            if (modulators[i] != null)
                if (i < modulationIndicies.Length)
                    if (i < indiciesLFO.Length && indiciesLFO[i] != null)
                        if (i < indiciesLFOModulation.Length)
                            phaseModulation = indiciesLFOModulation[i] * (indiciesLFO[i].generateData(time[voice]) * modulationIndicies[i] * modulators[i].GenerateData(voice) - phaseModulation) + phaseModulation;
                        else
                            phaseModulation += indiciesLFO[i].generateData(time[voice]) * modulationIndicies[i] * modulators[i].GenerateData(voice);
                    else
                        phaseModulation += modulationIndicies[i] * modulators[i].GenerateData(voice);
                else
                    phaseModulation += modulators[i].GenerateData(voice);

        data[voice] = GenerateEnvelopeScalar(voice) * velocity[voice] * modulatedAmplitude * Mathf.Sin((float) (4 * Mathf.PI * modulatedRatio * frequency[voice] * time[voice]) / sampleRate + 1f * phaseModulation + modulatedFeedback * data[voice]); //Note that the feedback has a delay on it.
        time[voice]++;
        recursion = false;
        return data[voice];
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
