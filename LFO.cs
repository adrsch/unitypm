using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LFO : MonoBehaviour
{
    public float frequency;
    private float sampleRate;

    public void SetSampleRate(float sampleRate)
    {
        this.sampleRate = sampleRate;
    }

    public float generateData(ulong time)
    {
        return Mathf.Cos((float)(4 * Mathf.PI * frequency * time) / sampleRate);
    }
}
