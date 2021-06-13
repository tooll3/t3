using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using T3.Core;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Gui.Interaction.PresetSystem;
using T3.Gui.Interaction.PresetSystem.Model;
using T3.Gui.Styling;

namespace T3.Gui.Windows
{
    public class PresetsWindow : Window
    {
        public PresetsWindow()
        {
            Config.Title = "Presets";
        }

        protected override void DrawContent()
        {
            DrawWindowContent();
        }

        public void DrawWindowContent()
        {
            var presetSystem = T3Ui.PresetSystem;
            var activeContext = presetSystem.ActiveContext;

            if (activeContext == null)
            {
                CustomComponents.EmptyWindowMessage("no preset groups");
                return;
            }
            
            if (CustomComponents.DisablableButton("save preset", activeContext.ActiveGroup != null))
            {
                presetSystem.AppendPresetToCurrentGroup();
            }

            var topLeftCorner = ImGui.GetCursorScreenPos();

            // Scene grid mode
            ImGui.PushFont(Fonts.FontSmall);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
            for (var groupIndex = 0; groupIndex < activeContext.Groups.Count; groupIndex++)
            {
                ImGui.PushID(groupIndex);
                ImGui.SetCursorScreenPos(topLeftCorner + new Vector2((ColumnWidth + SliderWidth + GroupPadding) * groupIndex, 0));
                ImGui.BeginGroup();

                var isActiveGroup = groupIndex == activeContext.ActiveGroupIndex;

                var group = activeContext.Groups[groupIndex];
                if (group == null)
                    continue;

                // Group column header
                ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
                ImGui.PushStyleColor(ImGuiCol.Text, isActiveGroup ? _activeGroupColor : _mutedTextColor.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, isActiveGroup ? new Color(0.1f) : Color.Transparent.Rgba);
                if (ImGui.Button($"{groupIndex}\n{group.Title}", new Vector2(ColumnWidth, GroupHeaderHeight)))
                {
                    presetSystem.ActivateGroupAtIndex(groupIndex);
                }

                ImGui.PopStyleColor(3);

                for (var sceneIndex = 0; sceneIndex < GridRows; sceneIndex++)
                {
                    Preset preset = null;

                    if (sceneIndex < activeContext.Presets.GetLength(1))
                        preset = activeContext.Presets[groupIndex, sceneIndex];

                    ImGui.PushID(sceneIndex);
                    var title = preset == null
                                    ? "-"
                                    : string.IsNullOrEmpty(preset.Title)
                                        ? $"{sceneIndex}"
                                        : preset.Title;

                    if (preset != null)
                    {
                        Color backgroundColor = _presetColor;
                        Color textColor = Color.Gray;
                        switch (preset.State)
                        {
                            case Preset.States.Active:
                                textColor = Color.White;
                                backgroundColor = _activePresetColor;
                                break;
                            case Preset.States.Modified:
                                textColor = Color.White;
                                backgroundColor = _activePresetColor;
                                break;
                            case Preset.States.IsBlended:
                                float f = group.BlendedPresets.IndexOf(preset) / (float)group.BlendedPresets.Count;
                                textColor = Color.White;

                                backgroundColor = f >= 0
                                                      ? Color.Mix(_blendStartColor, _blendEndColor, f)
                                                      : Color.Blue;
                                break;
                        }

                        ImGui.PushStyleColor(ImGuiCol.Button, backgroundColor.Rgba);
                        ImGui.PushStyleColor(ImGuiCol.Text, textColor.Rgba);

                        if (_renamePresetReference == preset)
                        {
                            var text = preset.Title;
                            ImGui.SetNextItemWidth(150);
                            ImGui.InputText("##input", ref text, 256);
                            ImGui.SetKeyboardFocusHere();
                            preset.Title = text;
                            
                            if ((ImGui.IsItemDeactivated() || ImGui.IsKeyPressed((int)Key.Return) || ImGui.IsMouseClicked(ImGuiMouseButton.Left)))
                            {
                                _renamePresetReference = null;
                                activeContext.WriteToJson();
                            }
                        }
                        
                        else if (ImGui.Button(title, new Vector2(ColumnWidth, GridRowHeight)))
                        {
                            if (ImGui.GetIO().KeyCtrl)
                            {
                                presetSystem.ActiveContext?.RemovePresetAtAddress(new PresetAddress(groupIndex, sceneIndex));
                            }
                            else if (ImGui.GetIO().KeyShift)
                            {
                                if (group.BlendedPresets.Contains(preset))
                                {
                                    group.BlendedPresets.Remove(preset);
                                    preset.State = Preset.States.InActive;
                                }
                                else
                                {
                                    if (group.BlendedPresets.Count == 0)
                                    {
                                        var lastActivePreset = group.ActivePreset;
                                        presetSystem.ActiveContext?.ActivatePreset(group, preset);

                                        if (lastActivePreset != null)
                                        {
                                            lastActivePreset.State = Preset.States.IsBlended;
                                            group.BlendedPresets.Add(lastActivePreset);
                                        }
                                    }

                                    group.BlendedPresets.Add(preset);
                                    preset.State = Preset.States.IsBlended;
                                }
                            }
                            else
                            {
                                presetSystem.ActiveContext?.ActivatePreset(group, preset);
                            }
                        }
                        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        {
                            _renamePresetReference = preset;
                        }         

                        ImGui.PopStyleColor(2);
                    }
                    else
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, _emptySlotColor.Rgba);
                        if (ImGui.Button("+", new Vector2(ColumnWidth, GridRowHeight)))
                        {
                            presetSystem.ActiveContext?.CreatePresetAtAddress(new PresetAddress(groupIndex, sceneIndex));
                        }

                        ImGui.PopStyleColor();
                    }

                    ImGui.PopID();
                }

                ImGui.EndGroup();
                ImGui.SameLine();
                ImGui.BeginGroup();
                {
                    ImGui.Dummy(new Vector2(SliderWidth, GroupHeaderHeight));
                    if (group.BlendedPresets.Count > 1)
                    {
                        if (DrawBlendSlider(ref _blendValue, group))
                        {
                            presetSystem.ActiveContext?.BlendGroupPresets(activeContext.ActiveGroup, _blendValue);
                        }
                    }
                    else
                    {
                        ImGui.SameLine(50);
                    }
                }
                ImGui.EndGroup();
                ImGui.SameLine();
                ImGui.PopID();
            }

            ImGui.PopStyleVar();
            ImGui.PopFont();

            // if (presetSystem.ActiveContext.ActiveGroup != null && activeContext.ActiveGroup.BlendedPresets.Count > 1)
            // {
            //     if (DrawBlendSlider(ref _blendValue))
            //     {
            //         presetSystem.BlendGroupPresets(activeContext.ActiveGroup, _blendValue);
            //     }
            // }
        }

        private static Preset _renamePresetReference;
        private static float _blendValue;

        private static bool DrawBlendSlider(ref float value, ParameterGroup group)
        {
            //const float sliderWidth = 30;
            var buttonSize = new Vector2(SliderWidth, SliderHeight);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, _presetColor.Rgba);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, _presetColor.Rgba);
            ImGui.PushStyleColor(ImGuiCol.Button, _presetColor.Rgba);
            ImGui.Button(".", buttonSize);
            ImGui.PopStyleColor(3);

            var edited = false;
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                ImGui.GetID(string.Empty);
                _previousValue = value;
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
            }

            var drawList = ImGui.GetWindowDrawList();
            var topLeft = ImGui.GetItemRectMin();
            var sliderPosition = topLeft + new Vector2(0, SliderHeight * value - 1);
            drawList.AddRectFilled(sliderPosition, sliderPosition + new Vector2(SliderWidth, 2), Color.Orange);
            
            // Draw Labels
            for (var index = 0; index < @group.BlendedPresets.Count; index++)
            {
                var f = index / ((float)group.BlendedPresets.Count-1);
                var preset = @group.BlendedPresets[index];

                var title = string.IsNullOrEmpty(preset.Title) ? "-" : preset.Title;
                drawList.AddText(new Vector2(topLeft.X, topLeft.Y + f * (SliderHeight - GridRowHeight)) , Color.White, title);
            }

            // interaction
            var isDragging = ImGui.IsMouseDragging(ImGuiMouseButton.Left) && ImGui.IsItemActive();
            if (isDragging)
            {
                value = (_previousValue + ImGui.GetMouseDragDelta().Y / SliderHeight).Clamp(0, 1);
                edited = true;
            }

            return edited;
        }

        private static float _previousValue;

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }

        private const float GroupHeaderHeight = 30;
        private const float SliderHeight = GridRows * (GridRowHeight+1);
        private const float ColumnWidth = 50;
        private const float SliderWidth = 30;
        private const float GroupPadding = 3;
        private const float GridRows = 16;
        private const float GridRowHeight = 14;

        private static readonly Color _activeGroupColor = Color.White;
        private static readonly Color _mutedTextColor = Color.Gray;
        private static readonly Color _activePresetColor = Color.Orange;
        private static readonly Color _emptySlotColor = Color.FromString("#0f0f0f");

        private static readonly Color _blendStartColor = Color.FromString("#ee01ba");
        private static readonly Color _blendEndColor = Color.FromString("#03afe9");
        private static readonly Color _presetColor = new Color(0.2f);
    }
}