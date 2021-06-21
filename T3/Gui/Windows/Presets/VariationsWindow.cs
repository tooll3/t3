using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using ImGuiNET;
using T3.Core;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Interaction.Variation;
using T3.Gui.Interaction.Variation.Model;
using T3.Gui.Styling;

namespace T3.Gui.Windows
{
    public class VariationsWindow : Window
    {
        public VariationsWindow()
        {
            Config.Title = "Variations";
        }

        protected override void DrawContent()
        {
            DrawWindowContent();
        }

        public void DrawWindowContent()
        {
            var presetSystem = T3Ui.VariationHandling;
            var activeOpVariation = presetSystem.ActiveOperatorVariation;

            if (activeOpVariation == null)
            {
                CustomComponents.EmptyWindowMessage("no preset groups");
                return;
            }

            // if (CustomComponents.DisablableButton("save preset", activeOpVariation.ActiveGroup != null))
            // {
            //     presetSystem.AppendPresetToCurrentGroup();
            // }

            var topLeftCorner = ImGui.GetCursorScreenPos();

            // Scene column mode
            ImGui.PushFont(Fonts.FontSmall);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
            if (presetSystem.ActiveOperatorVariation.IsGroupExpanded)
            {
                var groupIndex = presetSystem.ActiveOperatorVariation.ActiveGroupIndex;
                var group = presetSystem.ActiveOperatorVariation.ActiveGroup;
                if (group != null)
                {
                    ImGui.PushID(groupIndex);
                    ImGui.SetCursorScreenPos(topLeftCorner);
                    ImGui.BeginGroup();

                    // Group column header
                    ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
                    ImGui.PushStyleColor(ImGuiCol.Text, _activeGroupColor.Rgba);
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Color(0.1f).Rgba);
                    if (ImGui.Button($"{groupIndex}\n{group.Title}", new Vector2(ColumnWidth, GroupHeaderHeight)))
                    {
                        presetSystem.ActivateGroupAtIndex(groupIndex);
                    }

                    ImGui.PopStyleColor(3);

                    for (var sceneIndex = 0; sceneIndex < GroupRows * GridColumns; sceneIndex++)
                    {
                        DrawPresetToggle(sceneIndex, activeOpVariation, groupIndex, group, presetSystem);
                        if (sceneIndex == 0 || (sceneIndex + 1) % GridColumns > 0)
                            ImGui.SameLine();
                    }

                    ImGui.EndGroup();
                    
                    DrawBlendOrTransitionSlider(activeOpVariation, group, presetSystem);
                    
                    ImGui.PopID();
                }
            }
            else
            {
                for (var groupIndex = 0; groupIndex < activeOpVariation.Groups.Count; groupIndex++)
                {
                    ImGui.PushID(groupIndex);
                    ImGui.SetCursorScreenPos(topLeftCorner + new Vector2((ColumnWidth + SliderWidth + GroupPadding) * groupIndex, 0));
                    ImGui.BeginGroup();

                    var isActiveGroup = groupIndex == activeOpVariation.ActiveGroupIndex;

                    var group = activeOpVariation.Groups[groupIndex];
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

                    for (var sceneIndex = 0; sceneIndex < GroupRows; sceneIndex++)
                    {
                        DrawPresetToggle(sceneIndex, activeOpVariation, groupIndex, group, presetSystem);
                    }

                    ImGui.EndGroup();
                    DrawBlendOrTransitionSlider(activeOpVariation, group, presetSystem);

                    ImGui.PopID();
                }
            }

            ImGui.PopStyleVar();
            ImGui.PopFont();

            // if (variationHandling.ActiveOperatorVariation.ActiveGroup != null && activeContext.ActiveGroup.BlendedPresets.Count > 1)
            // {
            //     if (DrawBlendSlider(ref _blendValue))
            //     {
            //         variationHandling.BlendGroupPresets(activeContext.ActiveGroup, _blendValue);
            //     }
            // }
        }

        private static void DrawBlendOrTransitionSlider(OperatorVariation activeOpVariation, ParameterGroup group, VariationHandling presetSystem)
        {
            ImGui.SameLine();
            if (group.IsTransitionActive)
            {
                DrawBlendSlider(ref group.BlendTransitionProgress, activeOpVariation, group,
                                new List<Preset>() { group.BlendStartPreset, group.BlendTargetPreset });
            }
            else
            {
                if (DrawBlendSlider(ref _blendValue, activeOpVariation, group, group.BlendedPresets))
                    presetSystem.ActiveOperatorVariation?.BlendGroupPresets(activeOpVariation.ActiveGroup, _blendValue);
            }
        }

        private static void DrawPresetToggle(int sceneIndex, OperatorVariation activeOpVariation, int groupIndex, ParameterGroup group,
                                             VariationHandling presetSystem)
        {
            Preset preset = null;

            if (sceneIndex < activeOpVariation.Presets.GetLength(1))
                preset = activeOpVariation.Presets[groupIndex, sceneIndex];

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
                        backgroundColor = Color.Green;
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
                        activeOpVariation.WriteToJson();
                    }
                }

                else if (ImGui.Button(title, new Vector2(ColumnWidth, GridRowHeight)))
                {
                    if (ImGui.GetIO().KeyCtrl)
                    {
                        presetSystem.ActiveOperatorVariation?.RemovePresetAtAddress(new PresetAddress(groupIndex, sceneIndex));
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
                                presetSystem.ActiveOperatorVariation?.ActivatePreset(group, preset);

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
                        presetSystem.ActiveOperatorVariation?.ActivatePreset(group, preset);
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
                    presetSystem.ActiveOperatorVariation?.CreatePresetAtAddress(new PresetAddress(groupIndex, sceneIndex));
                }

                ImGui.PopStyleColor();
            }

            ImGui.PopID();
        }

        private static Preset _renamePresetReference;
        private static float[] BlendDurations = new[] { 0, 0.25f, 0.5f, 1, 2, 4, 16, 32, 64, 128 }; 
        
        private static bool DrawBlendSlider(ref float value, OperatorVariation activeOpVariation, ParameterGroup group,  List<Preset> presets)
        {
            var edited = false;
            ImGui.BeginGroup();
            {
                ImGui.Text(group.BlendTransitionDuration.ToString(CultureInfo.InvariantCulture));
                ImGui.SetNextWindowPos(ImGui.GetItemRectMin());
                CustomComponents.ContextMenuForItem(() =>
                                                    {
                                                        foreach (var d in BlendDurations)
                                                        {
                                                            var seconds = d * 240 / EvaluationContext.BPM;
                                                            var isCurrent = Math.Abs(group.BlendTransitionDuration - d) < 0.01f;
                                                            if (ImGui.MenuItem($"{d} bars", $"({seconds}s)", isCurrent))
                                                            {
                                                                group.BlendTransitionDuration = d;
                                                            }    
                                                        }
                                                        
                                                    }, null, "contextMenu", ImGuiPopupFlags.MouseButtonLeft);
                
                ImGui.Dummy(new Vector2(SliderWidth, GroupHeaderHeight - ImGui.GetFrameHeight()));

                if (presets.Count > 1)
                {
                    //const float sliderWidth = 30;
                    var buttonSize = new Vector2(SliderWidth, SliderHeight);
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, _presetColor.Rgba);
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, _presetColor.Rgba);
                    ImGui.PushStyleColor(ImGuiCol.Button, _presetColor.Rgba);
                    ImGui.Button("", buttonSize);
                    ImGui.PopStyleColor(3);

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
                    for (var index = 0; index < presets.Count; index++)
                    {
                        var f = index / ((float)presets.Count - 1);
                        var preset = presets[index];

                        var title = string.IsNullOrEmpty(preset.Title) ? "-" : preset.Title;
                        drawList.AddText(new Vector2(topLeft.X, topLeft.Y + f * (SliderHeight - GridRowHeight)), Color.White, title);
                    }

                    // interaction
                    var isDragging = ImGui.IsMouseDragging(ImGuiMouseButton.Left) && ImGui.IsItemActive();
                    if (isDragging)
                    {
                        value = (_previousValue + ImGui.GetMouseDragDelta().Y / SliderHeight).Clamp(0, 1);
                        edited = true;
                    }
                }
                else
                {
                    ImGui.SameLine(50);
                }
            }
            ImGui.EndGroup();

            return edited;
        }

        private static float _previousValue;

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }
        
        private static float _blendValue;

        private const float GroupHeaderHeight = 30;
        private const float SliderHeight = GroupRows * (GridRowHeight + 1);
        private const float ColumnWidth = 50;
        private const float SliderWidth = 30;
        private const float GroupPadding = 3;
        private const float GroupRows = 16;
        private const float GridColumns = 4;
        private const float GridRowHeight = 14;

        private static readonly Color _activeGroupColor = Color.White;
        private static readonly Color _mutedTextColor = Color.Gray;
        
        private static readonly Color _activePresetColor = Color.Orange;
        private static readonly Color _modifiedPresetColor = Color.Orange;
        private static readonly Color _emptySlotColor = Color.FromString("#0f0f0f");

        private static readonly Color _blendStartColor = Color.FromString("#ee01ba");
        private static readonly Color _blendEndColor = Color.FromString("#03afe9");
        private static readonly Color _presetColor = new Color(0.2f);
    }
}