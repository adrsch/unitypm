using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MidiController : MonoBehaviour
{
    public PMSynth pMSynth;
    private int[] voices = new int[15];
    private bool first = true;

    void NoteOn(byte pitch, byte velocity)
    {

    }


    void NoteOn(float pitch, byte velocity, int midiVoice)
    {
        int? voice = pMSynth.NoteOn(pitch, velocity);
        if (voice == null)
            Debug.Log("No voices available");
        else
            voices[midiVoice] = (int)voice;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (first && pMSynth.IsOn())
        {
            NoteOn(440f, 127, 0);
            first = false;

        }

    }
}
