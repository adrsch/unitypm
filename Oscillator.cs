using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Oscillator : MonoBehaviour
{
    // Oscillator Properties
    private float[] frequency; // Set as needed by PMSynth component.
    private float[] velocity; // Set as needed by PMSynth component.
    private bool[] voiceOn;
    private int voices;

    private bool[] playing;

    public float ratio = 1;
    public float amplitude = 1; // Range is 0 to 1. Use 0 to -1 for inversion. Values beyond 1 will clip.
    public float offset = 0; // Shifts the phase of the waves.

    private float[] data;
    private float sampleRate;
    private ulong[] time; // Will cause a click once this wraps around. Thankfully, this will not happen often, but it could be an issue for a continuously playing tone.

    // Modulators
    public Oscillator[] modulators = new Oscillator[0];
    public float[] modulationIndicies = new float[0]; // Optional. Acts as multiplier to the data of the corresponding modulator. Will default to 0, which is no modulation.

    // Envelope
    public float attack = 0;
    public float decay = 0;
    public float sustain = 1;
    public float release = 0;

    public LFO[] ratioLFO = new LFO[0];
    public float[] ratioLFOModulation = new float[0];
    public LFO[] amplitudeLFO = new LFO[0];
    public float[] amplitudeLFOModulation = new float[0];
    public LFO[] indiciesLFO = new LFO[0];
    public float[] indiciesLFOModulation = new float[0];

    private bool[] onRelease;
    private float[] offTime; // Used in calculating data during release
    private float[] offEnvelopeScalar; // Used in calculating data during release

    // Used to allow for feedback - previous datapoint will be used to calculate self modulation 
    private bool feedbackLoopCheck = false;
    
    private void setSampleRate(LFO[] lfos)
    {
        foreach (LFO lfo in lfos)
        {
            if (lfo == null) { continue; }
            lfo.SetSampleRate(sampleRate);
        }
    }

    private void setSampleRate(Oscillator[] oscillators)
    {
        foreach (Oscillator osc in oscillators)
        {
            if (osc == null) { continue; }
            osc.SetSampleRate(sampleRate);
        }
    }

    public void SetSampleRate(float sampleRate)
    {
        this.sampleRate = sampleRate;
        setSampleRate(modulators);
        setSampleRate(ratioLFO);
        setSampleRate(amplitudeLFO);
        setSampleRate(indiciesLFO);
    }

    public void SetVoices(int voices)
    {
        if (feedbackLoopCheck) { return; }
        feedbackLoopCheck = true;

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
        foreach (Oscillator osc in modulators)
        {
            if (osc == null) { continue; }
            osc.SetVoices(voices);
        }

        feedbackLoopCheck = false;
    }

    private float lfoModulation(
        LFO[] lfos,
        float[] indicies,
        float unmodulated,
        int voice)
    {
        float modulation = unmodulated;
        for (int i = 0; i < lfos.Length; i++)
        {
            if (lfos[i] == null) { continue; }

            // If modulation index not specified, use 1.
            float index = (i < indicies.Length)
                ? indicies[i]
                : 1f;

            modulation += index * (lfos[i].generateData(time[voice]) - modulation);
        }
        return modulation;
    }

    private float phaseModulation(int voice)
    {
        float phaseModulation = 0;
        for (int i = 0; i < modulators.Length; i++)
        {
            if (modulators[i] == null) { continue; }

            // If modulation index not specified, use 0.
            float index = (i < modulationIndicies.Length)
                ? modulationIndicies[i]
                : 0f;

            float modulatedIndex = lfoModulation(indiciesLFO, indiciesLFOModulation, index, voice);

            float modulatorData = new Func<float?, float>((float? data) =>
                (data != null)
                    ? (float)data
                    : 0f
            )(modulators[i].GenerateData(voice));
            
            phaseModulation +=
                modulatorData *
                modulatedIndex *
                frequency[voice] *
                modulators[i].ratio;
        }
        return phaseModulation;
    }


    public float? GenerateData(int voice)
    {
         // Check if the voice is inactive and can be reused.
        if (playing[voice] == false) { return null; }

        // Feeding an oscillator's modulator the oscillator itself will result in modulation using previous datapoint as is the case with feedback.
        if (feedbackLoopCheck) { return data[voice]; }
        feedbackLoopCheck = true;
        
        float modulatedRatio = lfoModulation(ratioLFO, ratioLFOModulation, ratio, voice);
        float modulatedAmplitude = lfoModulation(amplitudeLFO, amplitudeLFOModulation, amplitude, voice);

        float phaseModulation = this.phaseModulation(voice);

        // TODO: Replace with wavetable - faster & more flexible.
        data[voice] = 
            GenerateEnvelopeScalar(voice) *
            velocity[voice] *
            modulatedAmplitude *
            Mathf.Cos((float)
                (4 * Mathf.PI / sampleRate) *
                (modulatedRatio * frequency[voice] * time[voice] + phaseModulation)
            );
        time[voice]++;
        feedbackLoopCheck = false;
        return data[voice];
    }

    public float GenerateEnvelopeScalar(int voice)
    {
        float seconds = time[voice] / sampleRate;
        // NoteOff hasn't been sent, will either be in attack, decay, or sustain.
        if (onRelease[voice] == false)
        {
            seconds = time[voice] / sampleRate;
            // Attack
            if ( seconds - attack < 0)
            {
                return seconds / attack;
            }
            // Decay
            if ((seconds - attack) - decay < 0)
            {
                return 1 - (1- sustain) * (seconds - attack) / decay;
            }
            // Sustain
            if (sustain == 0)
            {
                playing[voice] = false;
            }
            return sustain;
        }
        // Release
        if (seconds - offTime[voice] < release)
        {
            return offEnvelopeScalar[voice] * (1 -  (seconds - offTime[voice]) / release);
        }
        // After release
        playing[voice] = false;
        return 0;
    }

    public void NoteOn(float frequency, float velocity, int voice)
    {
        if (feedbackLoopCheck) { return; }
        feedbackLoopCheck = true;

        playing[voice] = true;
        time[voice] = 0;
        this.frequency[voice] = frequency;
        this.velocity[voice] = velocity;
        foreach (Oscillator osc in modulators)
        {
            if (osc == null) { continue; }
            osc.NoteOn(frequency, velocity, voice);
        }

        feedbackLoopCheck = false;
    }

    public void NoteOff(int voice)
    {
        offTime[voice] = time[voice] / sampleRate;
        offEnvelopeScalar[voice] = GenerateEnvelopeScalar(voice);
        onRelease[voice] = true;
    }

}
