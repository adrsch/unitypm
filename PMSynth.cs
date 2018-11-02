using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PMSynth : MonoBehaviour 
{
    public float frequency;
    public float amplitude;
    private float sampleRate;
    public Oscillator.Oscillator[] outputOscillators = new Oscillator.Oscillator[0];

    void Start()
    {
        sampleRate = AudioSettings.outputSampleRate;
        for (int i = 0; i < outputOscillators.Length; i++)
        {
            outputOscillators[i].setFrequency(frequency);
            outputOscillators[i].setSampleRate(sampleRate);
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        for (int n = 0; n < data.Length / channels; n++)
        {
            //Sum up the outputs from all oscillators.
            float oscOutputs = 0;
            for (int j = 0; j < outputOscillators.Length; j++)
                oscOutputs += outputOscillators[j].generateData();

            //Currently, no stereo support.
            for (int i = 0; i < channels; i++)
                data[n * channels + i] = amplitude * oscOutputs;

            n++;
        }
    }
}