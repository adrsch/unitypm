using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FM : MonoBehaviour 
{
    public float frequency;
    public float amplitude;
    public float offset;
    private double sampleRate;
    private bool running = false;
    private Oscillator.Oscillator carrier;
    private Oscillator.Oscillator modulator;

    void Start()
    {
        double startTick = AudioSettings.dspTime;
        sampleRate = AudioSettings.outputSampleRate;
        modulator = new Oscillator.Oscillator(314f, amplitude, offset, sampleRate);
        carrier = new Oscillator.Oscillator(frequency, amplitude, offset, sampleRate, modulator);
        running = true;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!running)
            return;
        
        double sample = AudioSettings.dspTime * sampleRate;
        int dataLen = data.Length / channels;
        int n = 0;
        while (n < dataLen)
        {
            int i = 0;
            float dataPoint = carrier.generateData();
            while (i < channels)
            {
                data[n * channels + i] = dataPoint;
                i++;
            }
            n++;
        }
    }
}