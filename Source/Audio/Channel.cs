﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WaveTracker.Tracker;

namespace WaveTracker.Audio {
    public class Channel {

        const int OVERSAMPLE = 2;
        int id;
        public bool IsMuted { get; set; }
        public Wave currentWave;
        public int waveIndex { get; private set; }
        public List<TickEvent> tickEvents;
        float totalAmplitude {
            get { return tremoloMultiplier * (channelVolume / 99f) * (volumeEnv.Evaluate() / 99f); }
        } // * currentMacro.GetState().volumeMultiplier;
        float totalPitch => Math.Clamp(channelNotePorta + bendOffset + vibratoOffset + pitchFallOffset + arpeggioOffset + detuneOffset + arpEnvelopeResult, -12, 131);

        public float waveMorphPosition => waveMorphAmt / 99f;
        float bassBoost;

        int arpEnvelopeResult;
        /// <summary>
        /// value from 0.0-1.0, 0.5f is center
        /// </summary>
        float panValue = 0.5f;
        bool noteOn;

        int arpeggioOffset;
        int arpeggionote2;
        int arpeggionote3;
        int arpCounter;

        public float stereoPhaseOffset;

        float vibratoOffset; // 4xx command
        double vibratoTime;
        int volumeSlideSpeed;
        float vibratoSpeed;
        float vibratoIntensity;
        float tremoloMultiplier = 1f; // 7xx command
        float tremoloTime;
        float tremoloSpeed;
        float tremoloIntensity;
        float pitchFallOffset; // 1xx and 2xx commands
        float pitchFallSpeed; // 1xx and 2xx commands
        float portaSpeed; // 3xx command (in samples)
        float portaTime;
        float bendSpeed; // 3xx command (in samples)
        float bendOffset;
        int targetBendAmt;
        float channelNotePorta;
        float detuneOffset; // Pxx command
        int waveMorphAmt; // Ixx command
        float fmAmt; // Mxx command
        int waveBendAmt; // Jxx command
        int syncAmt; // Kxx command
        public int sampleStartOffset { get; private set; } // Yxx command
        int channelVolume; // volume column
        int channelNote; // notes column
        float lastNote;
        public Instrument currentMacro; // instrument column
        public EnvelopePlayer volumeEnv;
        public EnvelopePlayer arpEnv;
        public EnvelopePlayer pitchEnv;
        public EnvelopePlayer waveEnv;
        public EnvelopePlayer waveModEnv;
        public float _sampleVolume;
        float lastPitch;

        private float _frequency;
        private double _time;
        private float _leftAmp;
        private float _rightAmp;
        float _fadeMultiplier = 1;
        float _volumeSmooth;
        float _fmSmooth;
        public float coarseSampleVolume;
        public int tickNum;
        int macroID;
        private enum VoiceState { On, Off, Release }
        private VoiceState _state;
        public bool IsPlaying => _state == VoiceState.On;

        public Channel(int id) {
            this.id = id;
            tickEvents = new List<TickEvent>();
            Reset();
        }

        public float sampleTime => (float)_time;
        public int samplePlaybackPosition { get; private set; }

        public void QueueEvent(TickEventType eventType, int val1, int val2, int delay) {
            if (delay < 0) {
                DoEvent(new TickEvent(eventType, val1, val2, delay));
            }
            else {
                tickEvents.Add(new TickEvent(eventType, val1, val2, delay));
            }
        }

        public void ApplyEffect(char command, int parameter) {
            if (command == '4') // 4XY
            {
                if (parameter == 0)
                    vibratoTime = 0;
                vibratoSpeed = (parameter + 1) / 16;
                vibratoIntensity = parameter % 16;
            }
            if (command == '7') // 7XY
            {
                tremoloSpeed = (parameter + 1) / 16;
                tremoloIntensity = parameter % 16;
            }
            if (command == '8') // 8XX
            {
                SetPanning(parameter / 100f);
            }
            if (command == '1') // 1XX
            {
                pitchFallSpeed = parameter / 1f;
            }
            if (command == '2') // 2XX
            {
                pitchFallSpeed = -parameter / 1f;
            }
            if (command == '3') // 3XX
            {
                portaSpeed = parameter * 1.15f;
            }
            if (command == 'Q') // QXY
            {
                bendSpeed = (parameter / 16) * 11 + 1;
                targetBendAmt += parameter % 16;
            }
            if (command == 'R') // RXY
            {
                bendSpeed = (parameter / 16) * 11 + 1;
                targetBendAmt -= parameter % 16;
            }
            if (command == '0') //0XY
            {
                arpCounter = 0;
                arpeggionote2 = parameter / 16;
                arpeggionote3 = parameter % 16;
            }
            if (command == '9') //9XY
            {
                stereoPhaseOffset = parameter / 200f;
            }
            if (command == 'P') // PXX
            {
                detuneOffset = (parameter - 50) / 37.5f;
            }
            if (command == 'V') // VXX
            {
                SetWave(parameter);
            }
            if (command == 'A') // AXX
            {
                volumeSlideSpeed = -parameter;
            }
            if (command == 'W') // WXX
            {
                volumeSlideSpeed = parameter;
            }
            if (command == 'I') // IXX
            {
                waveMorphAmt = parameter;
            }
            if (command == 'M') // MXX
            {
                fmAmt = parameter / 20f;
            }
            if (command == 'J') // Jxx
            {
                waveBendAmt = parameter;
            }
            if (command == 'Y') {
                sampleStartOffset = parameter;
            }
            if (command == 'K') {
                syncAmt = parameter;
            }
        }

        public void SetVolume(int vol) {
            channelVolume = vol;
        }

        public void SetMacro(int id) {

            if (id < App.CurrentModule.Instruments.Count) {
                Instrument m = App.CurrentModule.Instruments[id];
                currentMacro = m;
                volumeEnv.toPlay = m.volumeEnvelope;
                arpEnv.toPlay = m.arpEnvelope;
                pitchEnv.toPlay = m.pitchEnvelope;
                waveEnv.toPlay = m.waveEnvelope;
                waveModEnv.toPlay = m.waveModEnvelope;
                if (macroID != id) {
                    macroID = id;
                    if (this.id < 0)
                        ResetModulations();
                    if (m.instrumentType == InstrumentType.Sample)
                        _time = 0;
                }
                volumeEnv.Start();
                pitchEnv.Start();
                arpEnv.Start();
                waveEnv.Start();
                waveModEnv.Start();
                if (currentMacro.instrumentType == InstrumentType.Wave) {
                    if (waveEnv.toPlay.isActive)
                        if (!waveEnv.EnvelopeEnded)
                            SetWave(waveEnv.Evaluate());
                    if (waveModEnv.toPlay.isActive)
                        if (!waveModEnv.EnvelopeEnded) {
                            if (currentMacro.waveModType == 0) {
                                waveMorphAmt = waveModEnv.Evaluate();
                            }
                            else if (currentMacro.waveModType == 1) {
                                waveBendAmt = waveModEnv.Evaluate();
                            }
                            else if (currentMacro.waveModType == 2) {
                                fmAmt = waveModEnv.Evaluate() / 20f;
                                _fmSmooth = fmAmt;
                            }
                        }
                }
            }

        }

        public void Release() {
            if (_state == VoiceState.On) {
                volumeEnv.Release();
                pitchEnv.Release();
                arpEnv.Release();
                waveEnv.Release();
                waveModEnv.Release();
                _state = VoiceState.Release;
            }
        }

        public void PreviewCut() {
            if (volumeEnv.toPlay.isActive && volumeEnv.toPlay.HasRelease)
                Release();
            else
                Cut();
        }

        public void Cut() {
            _state = VoiceState.Off;
        }

        public void OnNoteEnd() {
            vibratoTime = 0;
            arpCounter = 0;
            noteOn = false;
        }

        public void Reset() {
            currentMacro = new Instrument(InstrumentType.Wave);
            macroID = 0;
            volumeEnv = new EnvelopePlayer();
            arpEnv = new EnvelopePlayer();
            pitchEnv = new EnvelopePlayer();
            waveEnv = new EnvelopePlayer();
            waveModEnv = new EnvelopePlayer();
            tickEvents.Clear();
            noteOn = false;
            _state = VoiceState.Off;
            channelVolume = 99;
            detuneOffset = 0;
            pitchFallSpeed = 0;
            pitchFallOffset = 0;
            vibratoOffset = 0;
            volumeSlideSpeed = 0;
            _time = 0.0;
            arpEnvelopeResult = 0;
            arpCounter = 0;
            arpeggionote2 = 0;
            arpeggionote3 = 0;
            vibratoIntensity = 0;
            vibratoOffset = 0;
            vibratoSpeed = 0;
            vibratoTime = 0;
            tremoloIntensity = 0;
            tremoloMultiplier = 1f;
            tremoloSpeed = 0;
            tremoloTime = 0;
            stereoPhaseOffset = 0;
            waveMorphAmt = 0;
            fmAmt = 0;
            _fmSmooth = 0;
            waveBendAmt = 0;
            SetWave(0);
            lastNote = channelNote;
            channelNotePorta = channelNote;
            portaSpeed = 0;
            portaTime = 0;
            sampleStartOffset = 0;
            syncAmt = 0;
            bendSpeed = 0;
            targetBendAmt = 0;
            bendOffset = 0;
            SetPanning(0.5f);
        }

        public void ResetModulations() {
            waveMorphAmt = 0;
            _fmSmooth = 0;
            fmAmt = 0;
            waveBendAmt = 0;
        }

        public void SetWave(int waveIndex) {
            this.waveIndex = waveIndex;
            currentWave = ChannelManager.waveBank.GetWave(waveIndex);
        }


        public void TriggerNote(int midiNum) {
            pitchFallOffset = 0;
            if (!noteOn) {
                _fadeMultiplier = 1f;
            }
            if (_state == VoiceState.Off) {
                _time = 0.0;
            }
            if (_state == VoiceState.Release && volumeEnv.Evaluate() == 0)
                _time = 0.0;
            targetBendAmt = 0;
            bendOffset = 0;
            if (channelNote != midiNum || _state != VoiceState.On) {
                lastNote = channelNotePorta;
                channelNote = midiNum;
                if (portaSpeed == 0)
                    channelNotePorta = channelNote;
                portaTime = 0;
                noteOn = true;
                _state = VoiceState.On;
            }
            arpEnv.Start();
            arpEnvelopeResult = arpEnv.Evaluate();
            if (currentMacro.instrumentType == InstrumentType.Sample)
                _time = 0;
            _frequency = Helpers.NoteToFrequency(totalPitch);


        }
        private void SetPanning(float val) {

            if (val > 0.98f)
                val = 1.0f;
            panValue = val;
            _rightAmp = (float)Math.Sqrt(val);
            _leftAmp = (float)Math.Sqrt(1.0f - val);
        }
        /// <summary>
        /// The amplitude of this channel from 0.0 - 1.0
        /// </summary>
        public float CurrentAmplitude {
            get {
                if (currentMacro.instrumentType == InstrumentType.Wave)
                    return totalAmplitude * (_state == VoiceState.Off ? 0 : 1);
                else
                    return totalAmplitude * (_sampleVolume / 1.5f + 0.01f) * (_state == VoiceState.Off ? 0 : 1);
            }
        }

        public float CurrentAmplitudeAsWave {
            get {
                return totalAmplitude * (_state == VoiceState.Off ? 0 : 1);
            }
        }

        /// <summary>
        /// Pan value from 0.0-1.0, 0.5f is center
        /// </summary>
        public float CurrentPan { get { return panValue; } }

        /// <summary>
        /// This channel's current MIDI pitch
        /// </summary>
        public float CurrentPitch { get { return totalPitch; } }
        /// <summary>
        /// This channel's current frequency in hertz
        /// </summary>
        public float CurrentFrequency { get { return _frequency; } }

        public float EvaluateWave(float time) {
            // float t = time;
            float s = (syncAmt / 100f * 8) + 1;
            time = (s * (time % 1)) % 1;
            if (_fmSmooth > 0.001f)
                return currentWave.GetSampleMorphed(time + App.CurrentModule.WaveBank[(waveIndex + 1) % 100].GetSampleAtPosition(time) * (_fmSmooth * _fmSmooth) / 2f, App.CurrentModule.WaveBank[(waveIndex + 1) % 100], waveMorphAmt / 99f, waveBendAmt / 100f);
            else
                return currentWave.GetSampleMorphed(time, App.CurrentModule.WaveBank[(waveIndex + 1) % 100], waveMorphAmt / 99f, waveBendAmt / 100f);
        }



        void ContinuousTick(float deltaTime) {
            if (tremoloIntensity > 0) {
                tremoloTime += deltaTime * tremoloSpeed * 6f;
                tremoloMultiplier = 1f + (float)(Math.Sin(tremoloTime * 1) / 2 - 0.5f) * tremoloIntensity / 16f;
            }
            else {
                tremoloMultiplier = 1;
            }
            if (vibratoIntensity > 0) {
                vibratoTime += deltaTime * vibratoSpeed * 3f;
                vibratoOffset = (float)Math.Sin(vibratoTime * 2) * vibratoIntensity / 5f;
            }
            else
                vibratoOffset = 0;
            if (pitchFallSpeed != 0)
                pitchFallOffset += pitchFallSpeed * deltaTime * 2;
            if (portaTime < 2)
                portaTime += deltaTime * (portaSpeed == 0 ? AudioEngine.sampleRate : portaSpeed);

            if (channelNotePorta > channelNote) {
                channelNotePorta += (channelNote - lastNote) * deltaTime * portaSpeed;
                if (channelNotePorta < channelNote)
                    channelNotePorta = channelNote;
            }
            else if (channelNotePorta < channelNote) {
                channelNotePorta += (channelNote - lastNote) * deltaTime * portaSpeed;
                if (channelNotePorta > channelNote)
                    channelNotePorta = channelNote;
            }

            if (bendOffset > targetBendAmt) {
                bendOffset -= deltaTime * bendSpeed;
                if (bendOffset < targetBendAmt)
                    bendOffset = targetBendAmt;
            }
            else if (bendOffset < targetBendAmt) {
                bendOffset += deltaTime * bendSpeed;
                if (bendOffset > targetBendAmt)
                    bendOffset = targetBendAmt;
            }
            if (pitchEnv.toPlay.isActive && !pitchEnv.EnvelopeEnded)
                pitchFallOffset += pitchEnv.Evaluate() * deltaTime * 4;
            if (lastPitch != totalPitch) {
                lastPitch = totalPitch;
                _frequency = Helpers.NoteToFrequency(lastPitch);
            }

        }

        public void NextTick() {

            tickNum++;
            if (currentMacro.instrumentType == InstrumentType.Wave) {
                waveEnv.Step();
                if (waveEnv.toPlay.isActive)
                    if (!waveEnv.EnvelopeEnded && _state != VoiceState.Off)
                        SetWave(waveEnv.Evaluate());

            }
            if (currentMacro.instrumentType == InstrumentType.Wave) {
                waveModEnv.Step();
                if (waveModEnv.toPlay.isActive)
                    if (!waveModEnv.EnvelopeEnded && _state != VoiceState.Off) {
                        if (currentMacro.waveModType == 0) {
                            waveMorphAmt = waveModEnv.Evaluate();
                        }
                        else if (currentMacro.waveModType == 1) {
                            waveBendAmt = waveModEnv.Evaluate();
                        }
                        else if (currentMacro.waveModType == 2) {
                            fmAmt = waveModEnv.Evaluate() / 20f;
                        }
                    }

            }
            if (volumeEnv.toPlay.isActive)
                volumeEnv.Step();
            if (arpEnv.toPlay.isActive)
                arpEnv.Step();
            if (pitchEnv.toPlay.isActive)
                pitchEnv.Step();

            channelVolume += volumeSlideSpeed;
            if (channelVolume > 99)
                channelVolume = 99;
            if (channelVolume < 0)
                channelVolume = 0;
            for (int i = 0; i < tickEvents.Count; i++) {
                TickEvent t = tickEvents[i];
                if (t != null) {
                    if (t.countdown <= 0) {
                        DoEvent(t);
                        tickEvents.Remove(t);
                        i--;
                    }
                    else {
                        t.Update();
                    }
                }
            }
            if (arpeggionote2 == arpeggionote3 && arpeggionote2 == 0) {
                arpeggioOffset = 0;
            }
            else {
                switch (arpCounter) {
                    case 0:
                        arpeggioOffset = 0;
                        break;
                    case 1:
                        arpeggioOffset = arpeggionote2;
                        break;
                    case 2:
                        arpeggioOffset = arpeggionote3;
                        break;
                }
                arpCounter++;
                if (arpCounter > 2)
                    arpCounter = 0;
            }


            if (volumeEnv.toPlay.values.Count > 0 && volumeEnv.toPlay.isActive)
                try {
                    if (volumeEnv.EnvelopeEnded && volumeEnv.toPlay.values[volumeEnv.toPlay.values.Count - 1] == 0)
                        Cut();
                } catch {
                    // Cut();
                }
            arpEnvelopeResult = arpEnv.Evaluate();
            bassBoost = 0.6f * MathF.Pow(1.025f, -totalPitch) + 0.975f;
            //bassBoost = 1;

        }

        void DoEvent(TickEvent t) {
            if (t.eventType == TickEventType.Instrument) {
                SetMacro(t.value);
            }
            if (t.eventType == TickEventType.Note) {
                if (t.value == WTPattern.EVENT_NOTE_CUT)
                    Cut();
                else if (t.value == WTPattern.EVENT_NOTE_RELEASE)
                    Release();
                else if (t.value != WTPattern.EVENT_EMPTY)
                    TriggerNote(t.value);
            }
            if (t.eventType == TickEventType.Volume) {
                SetVolume(t.value);
            }
            if (t.eventType == TickEventType.Effect) {
                ApplyEffect((char)t.value, t.value2);
            }
        }

        public void ProcessSingleSample(out float left, out float right, bool continuousTick, float continuousDelta) {
            left = right = 0;

            float freqCut = 1;
            //if (_frequency > 15804) {
            //    //freqCut = 0;
            //    _frequency = 15804;
            //    //_state = VoiceState.Off;
            //}
            float delta = 1f / (OVERSAMPLE * AudioEngine.sampleRate) * _frequency;
            if (continuousTick)
                ContinuousTick(continuousDelta);
            if (noteOn) {
                float sampleSumL = 0;
                float sampleSumR = 0;
                float sampleL;
                float sampleR;
                for (int i = 0; i < 2; ++i) {
                    if (_state == VoiceState.Off) {
                        _fadeMultiplier /= 1.002f;
                        if (_fadeMultiplier < 0.001f) {
                            noteOn = false;
                        }
                    }
                    else {
                        _fadeMultiplier = 1;
                        _time += delta;
                    }
                    sampleL = 0;
                    sampleR = 0;
                    if (currentMacro != null) {
                        if (currentMacro.instrumentType == InstrumentType.Wave) {
                            if (_time > 1)
                                _time -= 1;
                            if (stereoPhaseOffset != 0) {
                                sampleL = EvaluateWave((float)_time - stereoPhaseOffset);
                                sampleR = EvaluateWave((float)_time + stereoPhaseOffset);
                            }
                            else {
                                sampleR = sampleL = EvaluateWave((float)_time);
                            }
                        }
                        else {
                            currentMacro.sample.SampleTick((float)_time, (int)(stereoPhaseOffset * 100), sampleStartOffset / 100f, out sampleL, out sampleR);
                            sampleL *= 1.25f;
                            sampleR *= 1.25f;
                            samplePlaybackPosition = currentMacro.sample.currentPlaybackPosition;
                            if (Math.Abs(sampleL) > _sampleVolume) {
                                _sampleVolume = Math.Abs(sampleL);
                            }
                            if (Math.Abs(sampleR) > _sampleVolume) {
                                _sampleVolume = Math.Abs(sampleR);
                            }
                        }
                    }
                    sampleSumL += sampleL;
                    sampleSumR += sampleR;
                }
                sampleL = sampleSumL / OVERSAMPLE;
                sampleR = sampleSumR / OVERSAMPLE;
                _volumeSmooth += (totalAmplitude - _volumeSmooth) * 0.02f;
                _fmSmooth += (fmAmt - _fmSmooth) * 0.05f;

                // float f = (float)Math.Pow(_volumeSmooth, 1.25);
                float f = _volumeSmooth;
                float l = sampleL * f * _leftAmp * _fadeMultiplier * freqCut;
                float r = sampleR * f * _rightAmp * _fadeMultiplier * freqCut;
                //if (AudioEngine.quantizeAmplitude) {
                //    if (_volumeSmooth < 0.005f) {
                //        l = 0;
                //        r = 0;
                //    }
                //    int quantamt = 8;
                //    l = (float)(Math.Ceiling(l * quantamt)) / (float)quantamt;
                //    r = (float)(Math.Ceiling(r * quantamt)) / (float)quantamt;
                //}
                left = l * 0.2f * bassBoost;
                right = r * 0.2f * bassBoost;
                if (id >= 0 && IsMuted) {
                    left = 0;
                    right = 0;
                }

            }

        }
    }
}
