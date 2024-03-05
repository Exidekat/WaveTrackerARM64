﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using WaveTracker.UI;
using WaveTracker.Tracker;
using System.Windows.Forms;

namespace WaveTracker.UI {
    public class WaveEditor : Element {
        public Texture2D tex;
        public static bool enabled;
        public SpriteButton presetSine, presetTria, presetSaw, presetRect50, presetRect25, presetRect12, presetRand, presetClear;
        public Toggle filterNone, filterLinear, filterMix;
        public int startcooldown;
        public SpriteButton closeButton;
        public UI.Button bCopy, bPaste, bPhaseL, bPhaseR, bMoveUp, bMoveDown, bInvert, bMutate, bSmooth, bNormalize;
        public UI.Textbox waveText;
        public Dropdown ResampleDropdown;
        public int id;
        int holdPosY, holdPosX;
        static string clipboardWave = "";
        static Audio.ResamplingModes clipboardSampleMode = Audio.ResamplingModes.Mix;
        int phase;
        public WaveEditor(Texture2D tex) {
            this.tex = tex;
            x = 220;
            y = 130;

            int buttonY = 23;
            int buttonX = 420;
            int buttonWidth = 64;


            bCopy = new UI.Button("Copy", buttonX, buttonY, this);
            bCopy.width = buttonWidth / 2 - 1;
            bCopy.isPartOfInternalDialog = true;
            bCopy.SetTooltip("", "Copy wave settings");
            buttonY += 0;
            bPaste = new UI.Button("Paste", buttonX + 32, buttonY, this);
            bPaste.width = buttonWidth / 2 - 1;
            bPaste.isPartOfInternalDialog = true;
            bPaste.SetTooltip("", "Paste wave settings");
            buttonY += 18;

            bPhaseR = new UI.Button("Phase »", buttonX, buttonY, this);
            bPhaseR.width = buttonWidth;
            bPhaseR.isPartOfInternalDialog = true;
            bPhaseR.SetTooltip("", "Shift phase once to the right");
            buttonY += 14;
            bPhaseL = new UI.Button("Phase «", buttonX, buttonY, this);
            bPhaseL.width = buttonWidth;
            bPhaseL.isPartOfInternalDialog = true;
            bPhaseL.SetTooltip("", "Shift phase once to the left");
            buttonY += 14;
            bMoveUp = new UI.Button("Shift Up", buttonX, buttonY, this);
            bMoveUp.width = buttonWidth;
            bMoveUp.isPartOfInternalDialog = true;
            bMoveUp.SetTooltip("", "Raise the wave 1 step up");
            buttonY += 14;
            bMoveDown = new UI.Button("Shift Down", buttonX, buttonY, this);
            bMoveDown.width = buttonWidth;
            bMoveDown.isPartOfInternalDialog = true;
            bMoveDown.SetTooltip("", "Lower the wave 1 step down");
            buttonY += 18;

            bInvert = new UI.Button("Invert", buttonX, buttonY, this);
            bInvert.width = buttonWidth;
            bInvert.isPartOfInternalDialog = true;
            bInvert.SetTooltip("", "Invert the wave vertically");
            buttonY += 14;
            bSmooth = new UI.Button("Smooth", buttonX, buttonY, this);
            bSmooth.width = buttonWidth;
            bSmooth.isPartOfInternalDialog = true;
            bSmooth.SetTooltip("", "Smooth out rough corners in the wave");
            buttonY += 14;
            bMutate = new UI.Button("Mutate", buttonX, buttonY, this);
            bMutate.width = buttonWidth;
            bMutate.isPartOfInternalDialog = true;
            bMutate.SetTooltip("", "Slightly randomize the wave");
            buttonY += 14;
            bNormalize = new UI.Button("Normalize", buttonX, buttonY, this);
            bNormalize.width = buttonWidth;
            bNormalize.isPartOfInternalDialog = true;
            bNormalize.SetTooltip("", "Make the wave maximum amplitude");


            presetSine = new SpriteButton(17, 215, 18, 12, tex, 0, this);
            presetSine.isPartOfInternalDialog = true;
            presetSine.SetTooltip("Sine", "Sine wave preset");

            presetTria = new SpriteButton(36, 215, 18, 12, tex, 1, this);
            presetTria.isPartOfInternalDialog = true;
            presetTria.SetTooltip("Triangle", "Triangle wave preset");
            presetSaw = new SpriteButton(55, 215, 18, 12, tex, 2, this);
            presetSaw.isPartOfInternalDialog = true;
            presetSaw.SetTooltip("Sawtooth", "Sawtooth wave preset");

            presetRect50 = new SpriteButton(74, 215, 18, 12, tex, 3, this);
            presetRect50.isPartOfInternalDialog = true;
            presetRect50.SetTooltip("Pulse 50%", "Pulse wave preset with 50% duty cycle");

            presetRect25 = new SpriteButton(93, 215, 18, 12, tex, 4, this);
            presetRect25.isPartOfInternalDialog = true;
            presetRect25.SetTooltip("Pulse 25%", "Pulse wave preset with 25% duty cycle");

            presetRect12 = new SpriteButton(112, 215, 18, 12, tex, 5, this);
            presetRect12.isPartOfInternalDialog = true;
            presetRect12.SetTooltip("Pulse 12.5%", "Pulse wave preset with 12.5% duty cycle");
            presetRand = new SpriteButton(131, 215, 18, 12, tex, 6, this);
            presetRand.SetTooltip("Random", "Create random noise");
            presetRand.isPartOfInternalDialog = true;
            presetClear = new SpriteButton(150, 215, 18, 12, tex, 7, this);
            presetClear.isPartOfInternalDialog = true;
            presetClear.SetTooltip("Clear", "Clear wave");

            closeButton = new SpriteButton(490, 0, 10, 9, UI.NumberBox.buttons, 4, this);
            closeButton.isPartOfInternalDialog = true;
            closeButton.SetTooltip("Close", "Close wave editor");

            waveText = new UI.Textbox("", 17, 188, 384, 384, this);
            waveText.isPartOfInternalDialog = true;
            waveText.canEdit = true;
            waveText.maxLength = 192;

            ResampleDropdown = new Dropdown(385, 215, this);
            ResampleDropdown.SetMenuItems(new string[] { "Harsh (None)", "Smooth (Linear)", "Mix (None + Linear)" });
        }
        public void EditWave(Wave wave, int num) {
            //Input.internalDialogIsOpen = true;
            startcooldown = 10;
            id = num;
            enabled = true;
            Input.focus = this;
        }

        public void Close() {
            enabled = false;
            //Input.internalDialogIsOpen = false;
            Input.focus = null;
        }

        public int pianoInput() {
            if (!enabled || !inFocus)
                return -1;
            if (MouseX < 10 || MouseX > 488 || MouseY > 258 || MouseY < 235)
                return -1;
            if (!Input.GetClick(KeyModifier.None))
                return -1;
            else {
                return (MouseX - 10) / 4;
            }
        }

        bool mouseInBounds() {
            return inFocus && canvasMouseX > 0 && canvasMouseY > 0 && canvasMouseX < 384 && canvasMouseY < 160;
        }
        int canvasMouseX => MouseX - 17;
        int canvasMouseY => MouseY - 23;
        int canvasPosX => canvasMouseX / 6;
        int canvasPosY => 31 - (canvasMouseY / 5);

        public void Update() {
            if (enabled) {
                if (WaveBank.currentWave < 0) return;
                if (WaveBank.currentWave > 99) return;
                if (Input.GetKeyRepeat(Microsoft.Xna.Framework.Input.Keys.Left, KeyModifier.None)) {
                    WaveBank.currentWave--;
                    if (WaveBank.currentWave < 0) {
                        WaveBank.currentWave += 100;
                    }
                    id = WaveBank.currentWave;
                }
                if (Input.GetKeyRepeat(Microsoft.Xna.Framework.Input.Keys.Right, KeyModifier.None)) {
                    WaveBank.currentWave++;
                    if (WaveBank.currentWave >= 100) {
                        WaveBank.currentWave -= 100;
                    }
                    id = WaveBank.currentWave;

                }
                WaveBank.lastSelectedWave = id;


                phase++;

                ResampleDropdown.Value = (int)Song.currentSong.waves[WaveBank.currentWave].resamplingMode;
                if (startcooldown > 0) {
                    waveText.Text = Song.currentSong.waves[WaveBank.currentWave].ToNumberString();
                    startcooldown--;
                } else {

                    if (closeButton.Clicked || Input.GetKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape, KeyModifier.None)) {
                        Close();
                    }
                    waveText.Update();
                    if (waveText.ValueWasChanged) {
                        Song.currentSong.waves[WaveBank.currentWave].SetWaveformFromNumber(waveText.Text);
                    } else {
                        waveText.Text = Song.currentSong.waves[WaveBank.currentWave].ToNumberString();
                    }

                    ResampleDropdown.Update();
                    Song.currentSong.waves[WaveBank.currentWave].resamplingMode = (Audio.ResamplingModes)ResampleDropdown.Value;
                    //if (filterNone.Clicked)
                    //    Song.currentSong.waves[WaveBank.currentWave].resamplingMode = Audio.ResamplingModes.None;
                    //if (filterLinear.Clicked)
                    //    Song.currentSong.waves[WaveBank.currentWave].resamplingMode = Audio.ResamplingModes.Linear;
                    //if (filterMix.Clicked)
                    //    Song.currentSong.waves[WaveBank.currentWave].resamplingMode = Audio.ResamplingModes.Mix;

                    if (presetSine.Clicked)
                        Song.currentSong.waves[WaveBank.currentWave].SetWaveformFromString("HJKMNOQRSTUUVVVVVVVUUTSRQONMKJHGECB9875432110000000112345789BCEF");
                    if (presetTria.Clicked)
                        Song.currentSong.waves[WaveBank.currentWave].SetWaveformFromString("GHIJKLMNOPQRSTUVVUTSRQPONMLKJIHGFEDCBA98765432100123456789ABCDEF");
                    if (presetSaw.Clicked)
                        Song.currentSong.waves[WaveBank.currentWave].SetWaveformFromString("GGHHIIJJKKLLMMNNOOPPQQRRSSTTUUVV00112233445566778899AABBCCDDEEFF");
                    if (presetRect50.Clicked)
                        Song.currentSong.waves[WaveBank.currentWave].SetWaveformFromString("VVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVV00000000000000000000000000000000");
                    if (presetRect25.Clicked)
                        Song.currentSong.waves[WaveBank.currentWave].SetWaveformFromString("VVVVVVVVVVVVVVVV000000000000000000000000000000000000000000000000");
                    if (presetRect12.Clicked)
                        Song.currentSong.waves[WaveBank.currentWave].SetWaveformFromString("VVVVVVVV00000000000000000000000000000000000000000000000000000000");

                    if (presetRand.Clicked)
                        Song.currentSong.waves[WaveBank.currentWave].Randomize();
                    if (presetClear.Clicked)
                        Song.currentSong.waves[WaveBank.currentWave].SetWaveformFromString("GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG");

                    if (bCopy.Clicked) {
                        clipboardWave = Song.currentSong.waves[WaveBank.currentWave].ToString();
                        clipboardSampleMode = Song.currentSong.waves[WaveBank.currentWave].resamplingMode;
                    }
                    if (bPaste.Clicked) {
                        if (clipboardWave.Length == 64) {
                            Song.currentSong.waves[WaveBank.currentWave].SetWaveformFromString(clipboardWave);
                            Song.currentSong.waves[WaveBank.currentWave].resamplingMode = clipboardSampleMode;
                        }
                    }

                    if (bPhaseL.Clicked)
                        Song.currentSong.waves[WaveBank.currentWave].ShiftPhase(1);

                    if (bPhaseR.Clicked)
                        Song.currentSong.waves[WaveBank.currentWave].ShiftPhase(-1);
                    if (bMoveUp.Clicked)
                        Song.currentSong.waves[WaveBank.currentWave].Move(1);
                    if (bMoveDown.Clicked)
                        Song.currentSong.waves[WaveBank.currentWave].Move(-1);
                    if (bInvert.Clicked)
                        Song.currentSong.waves[WaveBank.currentWave].Invert();
                    if (bSmooth.Clicked)
                        Song.currentSong.waves[WaveBank.currentWave].Smooth(2);
                    if (bMutate.Clicked)
                        Song.currentSong.waves[WaveBank.currentWave].Mutate();
                    if (bNormalize.Clicked)
                        Song.currentSong.waves[WaveBank.currentWave].Normalize();

                    if (mouseInBounds()) {
                        if (Input.GetClickDown(KeyModifier._Any)) {
                            holdPosX = canvasPosX;
                            holdPosY = canvasPosY;
                        }


                        if (Input.GetClick(KeyModifier.None)) {
                            Song.currentSong.waves[WaveBank.currentWave].samples[canvasPosX] = (byte)canvasPosY;
                        }
                        if (Input.GetClickUp(KeyModifier.Shift)) {
                            int diff = Math.Abs(holdPosX - canvasPosX);
                            if (diff > 0) {
                                if (holdPosX < canvasPosX) {
                                    for (int i = holdPosX; i <= canvasPosX; ++i) {
                                        Song.currentSong.waves[WaveBank.currentWave].samples[i] = (byte)Math.Round(Lerp(holdPosY, canvasPosY, (float)(i - holdPosX) / diff));
                                    }
                                } else {
                                    for (int i = canvasPosX; i <= holdPosX; ++i) {
                                        Song.currentSong.waves[WaveBank.currentWave].samples[i] = (byte)Math.Round(Lerp(canvasPosY, holdPosY, (float)(i - canvasPosX) / diff));
                                    }
                                }
                            } else {
                                Song.currentSong.waves[WaveBank.currentWave].samples[canvasPosX] = (byte)canvasPosY;
                            }
                        }
                    }
                }
            }
        }
        float Lerp(float firstFloat, float secondFloat, float by) {
            return firstFloat * (1 - by) + secondFloat * by;
        }
        public void Draw() {
            if (enabled) {
                DrawRect(-x, -y, 960, 600, Helpers.Alpha(Color.Black, 90));
                DrawSprite(tex, 0, 0, new Rectangle(0, 60, 500, 270));
                Write("Edit Wave " + id.ToString("D2"), 4, 1, new Color(64, 72, 115));
                closeButton.Draw();
                presetSine.Draw();
                presetTria.Draw();
                presetRect50.Draw();
                presetSaw.Draw();
                presetRect25.Draw();
                presetRect12.Draw();
                presetClear.Draw();
                presetRand.Draw();
                //filterNone.Draw();
                //filterLinear.Draw();
                //filterMix.Draw();
                waveText.Draw();

                bCopy.Draw();
                bPaste.Draw();
                bPhaseL.Draw();
                bPhaseR.Draw();
                bMoveUp.Draw();
                bMoveDown.Draw();
                bInvert.Draw();
                bSmooth.Draw();
                bMutate.Draw();
                bNormalize.Draw();
                ResampleDropdown.Draw();
                Color waveColor = new Color(200, 212, 93);
                Color waveBG = new Color(59, 125, 79, 150);
                for (int i = 0; i < 64; ++i) {
                    int samp = Song.currentSong.waves[WaveBank.currentWave].getSample(i);
                    int samp2 = Song.currentSong.waves[WaveBank.currentWave].getSample(i + phase);

                    DrawRect(17 + i * 6, 102, 6, -5 * (samp - 16), waveBG);
                    DrawRect(17 + i * 6, 183 - samp * 5, 6, -5, waveColor);
                    DrawRect(419 + i, 183, 1, 16 - samp2, new Color(190, 192, 211));
                    DrawRect(419 + i, 199 - samp2, 1, 1, new Color(118, 124, 163));
                }
                if (mouseInBounds() && inFocus) {
                    DrawRect(17 + (canvasMouseX / 6 * 6), 183 - ((31 - canvasMouseY / 5) * 5), 6, -5, Helpers.Alpha(Color.White, 80));
                    if (Input.GetClick(KeyModifier.Shift)) {
                        int diff = Math.Abs(holdPosX - canvasPosX);
                        if (diff > 0) {
                            if (holdPosX < canvasPosX) {
                                for (int i = holdPosX; i <= canvasPosX; ++i) {
                                    int y = (int)Math.Round(Lerp(holdPosY, canvasPosY, (float)(i - holdPosX) / diff));
                                    DrawRect(17 + i * 6, 183 - y * 5, 6, -5, Helpers.Alpha(Color.White, 80));
                                }
                            } else {
                                for (int i = canvasPosX; i <= holdPosX; ++i) {
                                    int y = (int)Math.Round(Lerp(canvasPosY, holdPosY, (float)(i - canvasPosX) / diff));
                                    DrawRect(17 + i * 6, 183 - y * 5, 6, -5, Helpers.Alpha(Color.White, 80));
                                }
                            }
                        }
                    }
                }

                if (App.pianoInput > -1) {
                    int note = App.pianoInput;
                    if (note >= 0 && note < 120) {
                        if (Helpers.IsNoteBlackKey(App.pianoInput))
                            DrawSprite(tex, App.pianoInput * 4 + 10, 235, new Rectangle(504, 61, 4, 24));
                        else
                            DrawSprite(tex, App.pianoInput * 4 + 10, 235, new Rectangle(500, 61, 4, 24));
                    }
                }
            }
        }
    }
}
