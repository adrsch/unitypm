using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oscillator
{
    public class Oscillator
    {
        public float frequency { get; set; }
        public float amplitude { get; set; }
        public float offset { get; set; }
        public float sampleRate { get; set; }
        private ulong time = 0; //Will jump once this wraps around. Thankfully, this will not happen often, but it could be an issue for a continuously playing tone.
        private Oscillator modulator = null;

        public Oscillator(float frequency, float amplitude, float offset, double sampleRate)
        {
            this.frequency = frequency;
            this.amplitude = amplitude;
            this.offset = offset;
            this.sampleRate = (float) sampleRate;
        }

        public Oscillator(float frequency, float amplitude, float offset, double sampleRate, Oscillator modulator)
        {
            this.frequency = frequency;
            this.amplitude = amplitude;
            this.offset = offset;
            this.sampleRate = (float)sampleRate;
            this.modulator = modulator;
        }

        public float generateData()
        {
            float data;
            if (modulator == null)
            {
                data = amplitude * Mathf.Sin((2 * Mathf.PI * frequency * time) / sampleRate);
            }
            else
            {
                data = amplitude * Mathf.Sin((2 * Mathf.PI * frequency * time) / sampleRate + 1f * modulator.generateData());
            }
            time++;
            return data;
        }
    }
}
