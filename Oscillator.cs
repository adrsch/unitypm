using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oscillator
{
    public class Oscillator : MonoBehaviour
    {
        private float frequency; //Set as needed by PMSynth component 
        public float ratio;
        public float amplitude;
        public float offset;
        public float feedback;

        private float data = 0;
        private float sampleRate;
        private ulong time = 0; //Will cause a click once this wraps around. Thankfully, this will not happen often, but it could be an issue for a continuously playing tone.
        public Oscillator[] modulators = new Oscillator[0];
        
        public void setSampleRate(float sampleRate)
        {
            this.sampleRate = sampleRate;
            for (int i = 0; i < modulators.Length; i++)
                modulators[i].sampleRate = sampleRate;
        }

        public void setFrequency(float frequency)
        {
            this.frequency = frequency;
            for (int i = 0; i < modulators.Length; i++)
                if (modulators[i] != null)
                    modulators[i].setFrequency(frequency);
        }

        public float generateData()
        {
            float phaseModulation = 0;
            for (int i = 0; i < modulators.Length; i++)
                phaseModulation += modulators[i].generateData();
            data = amplitude * Mathf.Sin((2 * Mathf.PI * ratio * frequency * time) / sampleRate + 1f * phaseModulation + feedback*data + offset); //Note that the feedback has a delay on it.
            time++;
            return data;
        }
    }
}
