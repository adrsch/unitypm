﻿using System.Collections;
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


    void NoteOn(float pitch, float velocity, int midiVoice)
    {
        int? voice = pMSynth.NoteOn(pitch, velocity);
        if (voice == null)
            Debug.Log("No voices available");
        else
            voices[midiVoice] = (int)voice;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (first)
        {
            NoteOn(440f, 1, 0);
            /*
            NoteOn(98.00f, 127, 0);
            NoteOn(220.00f, 127, 0);
            NoteOn(261.63f, 127, 0);
            NoteOn(329.63f, 127, 0);
            NoteOn(392.00f, 127, 0);
            NoteOn(493.88f, 127, 0);
            NoteOn(587.33f, 127, 0);
            */
            first = false;

        }

    }
}
