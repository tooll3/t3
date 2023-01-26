using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using ImGuiNET;
using ManagedBass;
using Newtonsoft.Json;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Gui.Audio;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Interaction.Timing;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Windows.TimeLine
{
    public static class PlaybackSettingsPopup
    {
        public static readonly string PlaybackSettingsPopupId = "##PlaybackSettings";

        public static void DrawPlaybackSettings(ref Playback playback)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 6));
            ImGui.SetNextWindowSize(new Vector2(600, 400));
            if (!ImGui.BeginPopupContextItem(PlaybackSettingsPopupId))
            {
                ImGui.PopStyleVar(2);
                return;
            }

            T3Ui.OpenedPopUpName = PlaybackSettingsPopupId;

            ImGui.PushFont(Fonts.FontLarge);
            ImGui.TextUnformatted("Playback settings");
            ImGui.PopFont();

            var composition = GraphWindow.GetMainComposition();
            if (composition == null)
            {
                CustomComponents.EmptyWindowMessage("no composition active");
                ImGui.EndPopup();
                return;
            }

            var hasCompositionSoundtrack = composition.Symbol.SoundSettings != null && composition.Symbol.SoundSettings.HasSettings;
            FormInputs.SetIndent(0);
            if (FormInputs.DrawCheckBox("Specify settings for", ref hasCompositionSoundtrack))
            {
                if (hasCompositionSoundtrack)
                {
                    composition.Symbol.SoundSettings ??= new SoundSettings();
                    composition.Symbol.SoundSettings.HasSettings = true;
                }
                else
                {
                    composition.Symbol.SoundSettings.HasSettings = false;
                }
            }

            ImGui.SameLine();
            ImGui.PushFont(Fonts.FontBold);
            ImGui.TextUnformatted(composition.Symbol.Name);
            ImGui.PopFont();

            var compWithSoundtrack = GetFindSoundtrackComposition(composition);
            var compositionHasSoundtrack = compWithSoundtrack == composition;
            var settingsAreInherrited = false;

            string hint = "";

            if (composition.Symbol.SoundSettings != null && compositionHasSoundtrack && composition.Symbol.SoundSettings.HasSettings)
            {
                hint = "Your defining new settings for this Project Operator.";
            }
            else if (compWithSoundtrack != null)
            {
                hint = $"Inheriting settings from {compWithSoundtrack.Symbol.Name}";
                settingsAreInherrited = true;
            }
            else
            {
                hint = string.Empty;
            }

            FormInputs.DrawHint(hint);

            FormInputs.ResetIndent();

            if (settingsAreInherrited)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.2f);
            }

            if (ImGui.BeginTabBar("##timeMode"))
            {
                if (ImGui.BeginTabItem("AudioFile"))
                {
                    ImGui.TextUnformatted("Soundtrack");

                    //var composition = GraphWindow.GetMainComposition();
                    if (!SoundtrackUtils.TryFindingSoundtrack(composition, out var soundtrack))
                    {
                        if (ImGui.Button("Add soundtrack to composition"))
                        {
                            composition.Symbol.SoundSettings.AudioClips.Add(new AudioClip
                                                                                {
                                                                                    IsSoundtrack = true,
                                                                                });
                        }
                    }
                    else
                    {
                        var filepathModified = FormInputs.FilePicker("Soundtrack",
                                                                     ref soundtrack.FilePath,
                                                                     "filepath to soundtrack",
                                                                     null,
                                                                     FileOperations.FilePickerTypes.File
                                                                    );
                        // var filepathModified =
                        //     FileOperations.DrawSoundFilePicker(FileOperations.FilePickerTypes.File, ref soundtrack.FilePath);

                        FormInputs.ApplyIndent();
                        if (ImGui.Button("Reload"))
                        {
                            AudioEngine.ReloadClip(soundtrack);
                        }

                        ImGui.SameLine();
                        if (ImGui.Button("Remove"))
                        {
                            composition.Symbol.SoundSettings.AudioClips.Remove(soundtrack);
                        }

                        if (FormInputs.DrawFloatField("BPM", 
                                                      ref soundtrack.Bpm,
                                                      0,
                                                      1000,
                                                      0.02f,
                                                      true,
                                                      "In T3 animation units are in bars.\nThe BPM rate controls the animation speed of your project.",
                                                      120))
                        {
                            playback.Bpm = soundtrack.Bpm;
                        }

                        var soundtrackStartTime = (float)soundtrack.StartTime;
                        
                        if (FormInputs.DrawFloatField("Offset", 
                                                      ref soundtrackStartTime,
                                                      -100,
                                                      100,
                                                      0.02f,
                                                      false,
                                                      "Offsets the beginning of the soundtrack in seconds.",
                                                      0))
                        {
                            soundtrack.StartTime = soundtrackStartTime;
                        }

                        if (FormInputs.DrawFloatField("Resync Threshold", 
                                                      ref ProjectSettings.Config.AudioResyncThreshold,
                                                      0.001f,
                                                      0.1f,
                                                      0.001f,
                                                      true,
                                                      "If audio playbacks drifts too far from the animation playback it will be resynced. If the threshold for this is too low you will encounter audio glitches. If the threshold is too large you will lose precision. A normal range is between 0.02s and 0.05s.",
                                                      0.02f))

                        //ImGui.SetNextItemWidth(150);
                        //if (ImGui.DragFloat("Resync Threshold in Seconds", ref ProjectSettings.Config.AudioResyncThreshold, 0.001f, 0.01f, 1f))
                        {
                            UserSettings.Save();
                        }

                        if (filepathModified)
                        {
                            UpdateBpmFromSoundtrackConfig(soundtrack);
                        }
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Tapping"))
                {
                    CustomComponents.HelpText("Tab the Sync button to set begin of measure and to improve BPM detection.");
                    var isInitialized = playback is BeatTimingPlayback;
                    if (isInitialized)
                    {
                    }
                    else
                    {
                        if (ImGui.Button("Initialize"))
                        {
                            playback = new BeatTimingPlayback();
                        }
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Audio input"))
                {
                    ImGui.DragFloat("AudioGain", ref ProjectSettings.Config.AudioGainFactor, 0.01f, 0, 100);
                    ImGui.DragFloat("AudioDecay", ref ProjectSettings.Config.AudioDecayFactor, 0.001f, 0, 1);

                    ImGui.Spacing();
                    ImGui.TextColored(Color.Gray, "Select source for audio analysis...");
                    if (ImGui.Selectable("Internal Soundtrack", string.IsNullOrEmpty(ProjectSettings.Config.AudioInputDeviceName)))
                    {
                        ProjectSettings.Config.AudioInputDeviceName = string.Empty;
                        AudioAnalysis.InputMode = AudioAnalysis.InputModes.Soundtrack;
                        Bass.Configure(Configuration.UpdateThreads, true);
                    }

                    if (!WasapiAudioInput.DevicesInitialized)
                    {
                        ImGui.Spacing();
                        if (ImGui.Button("Init sound input devices"))
                        {
                            WasapiAudioInput.InitializeInputDeviceList();
                        }

                        CustomComponents.HelpText("Scanning WASAPI input devices can take several seconds...");
                    }
                    else
                    {
                        foreach (var d in WasapiAudioInput.InputDevices)
                        {
                            var isSelected = d.DeviceInfo.Name == ProjectSettings.Config.AudioInputDeviceName;
                            if (ImGui.Selectable($"{d.DeviceInfo.Name}", isSelected))
                            {
                                Bass.Configure(Configuration.UpdateThreads, false);

                                ProjectSettings.Config.AudioInputDeviceName = d.DeviceInfo.Name;
                                ProjectSettings.Save();
                                WasapiAudioInput.StartInputCapture(d);
                            }

                            var di = d.DeviceInfo;
                            var j = JsonConvert.SerializeObject(di);

                            CustomComponents.TooltipForLastItem($"{j}");
                        }
                    }
                }

                if (ImGui.BeginTabItem("OSC"))
                {
                    CustomComponents.HelpText("Use OSC to send events to /beatTimer on every beat.");
                    //var isInitialized = playback is BeatTimingPlayback;
                    if (OscBeatTiming.Initialized)
                    {
                        ImGui.TextUnformatted($"Last received beat {OscBeatTiming.BeatCounter}");
                    }
                    else
                    {
                        if (ImGui.Button("Initialize"))
                        {
                            OscBeatTiming.Init();
                            playback = new BeatTimingPlayback();
                        }
                    }

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            if (settingsAreInherrited)
            {
                ImGui.PopStyleVar();
            }

            ImGui.EndPopup();
            ImGui.PopStyleVar(2);
        }

        private static void UpdateBpmFromSoundtrackConfig(AudioClip audioClip)
        {
            var matchBpmPattern = new Regex(@"(\d+\.?\d*)bpm");
            var result = matchBpmPattern.Match(audioClip.FilePath);
            if (!result.Success)
                return;

            if (float.TryParse(result.Groups[1].Value, out var bpm))
            {
                Log.Debug($"Using bpm-rate {bpm} from filename.");
                audioClip.Bpm = bpm;
            }
        }

        public static Instance GetFindSoundtrackComposition(Instance composition)
        {
            while (true)
            {
                var soundtrackSymbol = composition.Symbol;
                var soundtrack = soundtrackSymbol.SoundSettings.AudioClips.SingleOrDefault(ac => ac.IsSoundtrack);
                if (soundtrack != null)
                {
                    return composition;
                }

                if (composition.Parent == null)
                {
                    return null;
                }

                composition = composition.Parent;
            }
        }
    }
}