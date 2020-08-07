# UnityPM: Phase Modulation Synthesis for Unity
Phase modulation, functionally equivalent to frequency modulation, is a powerful and flexible audio synthesis technique. UnityPM brings highly flexible real-time PM synthesis to the Unity game engine.
## Installation
Place all files somewhere inside of your Unity project directory (ie in a unitypm folder).

The latest version testing has been done on is 2017.4.24f1 LTS. If you are running into issues and running a newer (or older) Unity version, that may be the cause.

Additionally, for Linux users, make sure to install all the required dependencies for Unity.

## Usage
### Getting started

Attach UnityPM.cs to a Unity GameObject with an audio source present.

To add oscillators, drag Oscillator.cs as many times as is desired. Next, set the size of the Oscillators list in the UnityPM component to match, and drag each of the oscillators into the list.

Make sure to set the amplitude and number of voices in the UnityPM component!

While this fully sets up a PMSynth, it is necessary to call the NoteOn and NoteOff functions for audio to play.

### Playing sound

Create a controller class, with PMController.cs as an example, that contains a PMSynth Component.
```
public PMSynth pMSynth;
```
Call PMSynth's NoteOn function to send a signal to play a note. This will return an int? containing the voice number used for playing the note. This will be null if no voices are available.
```
int? voice;
voice = pMSynth.NoteOn(frequency, velocity);
```
The velocity uses midi notation: 0 is silent, 127 is max.

To stop the note a voice is playing, use NoteOff.
```
pMSynth.NoteOff(voice);
```
### Modulation

Set the modulation list size in an Oscillator component to the desired number of modulators, then drag Oscillator components to fill the list.

To have an oscillator modulate itself, use the feedback field.

The modulation indicies list should match the side of the modulators list, and contains the amount of modulation for each modulator.

Additionally, the offset, ratio, and amplitude of an Oscillator component can be set to change the sound.

### LFO

LFO is handled using separate LFO components, using LFO.cs.

Set the frequency of an LFO component as is desired, then drag it to any of the LFO fields in an Oscillator component to set the LFO. It is possible for LFO to modulate the ratio, amplitude, feedback, and modulation indicies. Any number of LFOs can be used - set the size of the list to however many are to be used.
