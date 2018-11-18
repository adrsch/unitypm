using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PMSynth : MonoBehaviour 
{
    public float frequency;
    public float amplitude;
    public byte voices;
    private float sampleRate;
    public Oscillator[] outputOscillators = new Oscillator[0];
    private bool[] activeVoices;

    void Start()
    {
        activeVoices = new bool[voices];
        sampleRate = AudioSettings.outputSampleRate;
        for (int i = 0; i < outputOscillators.Length; i++)
        {
            outputOscillators[i].SetSampleRate(sampleRate);
            outputOscillators[i].SetVoices(voices);
        }

        Application.targetFrameRate = 10;
    }

    void NoteOn(float frequency, byte velocity)
    {

    }

    void NoteOn(float frequency)
    {

    }

    void NoteOn(byte pitch, byte velocity)
    {

    }

    void NoteOff()
    {

    }

    private int t = 0;
    void OnAudioFilterRead(float[] data, int channels)
    {
         if (t > 100000000)
            t = 0;

        for (int n = 0; n < data.Length / channels; n++)
        {
            //Sum up the outputs from all oscillators.
            float oscOutputs = 0;
            for (int j = 0; j < outputOscillators.Length; j++)
            {
                if (t == 0)
                {
                    int voiceUsed = outputOscillators[j].NoteOn(frequency, 255);
                    if (voiceUsed != 256)
                    {
                        activeVoices[voiceUsed] = true;
                    }
                }
                if (t == 100000)
                {
                    for (byte i = 0; i < voices; i++)
                        if (activeVoices[i] == true)
                            outputOscillators[i].NoteOff(i);

                }
                for (byte i = 0; i < voices; i++)
                    if (activeVoices[i] == true)
                    {
                        float oscData = outputOscillators[j].GenerateData(i);
                        if (float.IsNegativeInfinity(oscData))
                            activeVoices[i] = false;
                        else
                            oscOutputs += oscData;
                    }
            }
            //Currently, no stereo support.
            for (int i = 0; i < channels; i++)
                data[n * channels + i] = amplitude * oscOutputs;

            n++;


            t++;
        }
    }
}