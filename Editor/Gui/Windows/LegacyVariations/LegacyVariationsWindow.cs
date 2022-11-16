using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Editor.Gui.Interaction.LegacyVariations;
using Editor.Gui.Interaction.LegacyVariations.Model;
using ImGuiNET;
using T3.Core;
using T3.Core.Animation;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Operator;
using Editor.Gui;
using Editor.Gui.Styling;
using Editor.Gui.Windows;
using T3.Core.Resource;
using T3.Core.Utils;
using T3.Editor.Gui.Interaction.LegacyVariations;
using T3.Editor.Gui.Interaction.LegacyVariations.Model;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.Windows;

namespace Editor.Gui.Windows.Variations
{
    public class LegacyVariationsWindow : Window
    {
        public LegacyVariationsWindow()
        {
            Config.Title = "Legacy Variations";
        }

        protected override void DrawContent()
        {
            DrawWindowContent();
        }

        public void DrawWindowContent()
        {
            // var activeVariation = T3Ui.VariationHandling.ActiveOperatorVariation;
            // if (activeVariation == null || activeVariation.Groups.Count == 0)
            // {
            //     if (CustomComponents.EmptyWindowMessage("no variation groups\nfor symbol", "Create group"))
            //     {
            //         T3Ui.VariationHandling.ShowAddGroupDialog();
            //     }
            //
            //     return;
            // }
            //
            // var width = ImGui.GetContentRegionAvail().X;
            // const float groupDetailsWidth = 200;
            //
            // if (ImGui.BeginChild("groups", new Vector2(width - groupDetailsWidth, -1)))
            // {
            //     DrawCompositionGroups();
            // }
            //
            // ImGui.EndChild();
            //
            // ImGui.SameLine();
            // if (ImGui.BeginChild("groupDetails", new Vector2(groupDetailsWidth, -1)))
            // {
            //     var activeGroup = activeVariation.ActiveGroup;
            //     if (activeGroup == null)
            //     {
            //         CustomComponents.EmptyWindowMessage("No group selected");
            //         return;
            //     }
            //
            //     var activePreset = activeGroup.ActivePreset;
            //
            //     // Group Title
            //     ImGui.PushFont(Fonts.FontLarge);
            //     ImGui.TextUnformatted(activeGroup.Title);
            //     ImGui.PopFont();
            //
            //     // Functions
            //     ImGui.SameLine(groupDetailsWidth - 16);
            //     if (CustomComponents.IconButton(Icon.Trash, "", new Vector2(16, 16)))
            //     {
            //         ImGui.OpenPopup("Delete?");
            //         
            //     }
            //     
            //     if (ImGui.BeginPopupModal("Delete?"))
            //     {
            //         ImGui.TextUnformatted("Delete this Variation group?.\nThis operation cannot be undone!\n\n");
            //         ImGui.Separator();
            //
            //         if (ImGui.Button("OK", new Vector2(120, 0)))
            //         {
            //             ImGui.CloseCurrentPopup();
            //             activeVariation.RemoveGroup(activeGroup);
            //         }
            //         ImGui.SetItemDefaultFocus();
            //         ImGui.SameLine();
            //         if (ImGui.Button("Cancel", new Vector2(120, 0))) { ImGui.CloseCurrentPopup(); }
            //         ImGui.EndPopup();
            //     }
            //     
            //
            //     // Parameters
            //     Guid symbolChildId = Guid.Empty;
            //     SymbolChild lastSymbolChild = null;
            //     var obsoleteParameters = new List<GroupParameter>();
            //     foreach (var param in activeGroup.Parameters)
            //     {
            //         if (param.SymbolChildId != symbolChildId)
            //         {
            //             lastSymbolChild = activeVariation.CompositionInstance.Symbol.Children.SingleOrDefault(child => child.Id == param.SymbolChildId);
            //             if (lastSymbolChild == null)
            //             {
            //                 Log.Warning($"Discarding obsolete variation parameter: {param.Title}");
            //                 obsoleteParameters.Add(param);
            //                 continue;
            //             }
            //
            //             symbolChildId = lastSymbolChild.Id;
            //
            //             ImGui.PushFont(Fonts.FontSmall);
            //             ImGui.PushStyleColor(ImGuiCol.Text, T3Style.Colors.TextMuted.Rgba);
            //             ImGui.TextUnformatted(lastSymbolChild.ReadableName);
            //             ImGui.PopStyleColor();
            //             ImGui.PopFont();
            //             if (ImGui.IsItemHovered())
            //             {
            //                 ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            //                 T3Ui.AddHoveredId(symbolChildId);
            //                 if (ImGui.IsItemClicked())
            //                 {
            //                     T3Ui.CenterHoveredId(symbolChildId);
            //                 }
            //             }
            //         }
            //
            //         if (lastSymbolChild != null && lastSymbolChild.InputValues.TryGetValue(param.InputId, out var input))
            //         {
            //             ImGui.TextUnformatted(input.Name);
            //
            //             var valueInPreset = activePreset?.ValuesForGroupParameterIds[param.Id];
            //             if (input.Value is InputValue<float> floatValue
            //                 && valueInPreset is InputValue<float> orgValue)
            //             {
            //                 var isModified = Math.Abs(floatValue.Value - orgValue.Value) > 0.0001f;
            //                 var currentValue = floatValue.Value;
            //
            //                 var highlightIfModified = isModified ? Color.Orange.Rgba : Color.Gray;
            //                 ImGui.SameLine(100);
            //                 ImGui.PushStyleColor(ImGuiCol.Text, highlightIfModified);
            //                 ImGui.TextUnformatted($"{currentValue:G4}");
            //                 ImGui.PopStyleColor();
            //             }
            //         }
            //     }
            //     
            //     for (var index = obsoleteParameters.Count - 1; index >= 0; index--)
            //     {
            //         activeGroup.Parameters.RemoveAt(index);
            //     }
            // }
            //
            // ImGui.EndChild();
        }

        // private static void DrawCompositionGroups()
        // {
        //     var variationHandling = T3Ui.VariationHandling;
        //     var activeOpVariation = variationHandling.ActiveOperatorVariation;
        //
        //     // if (CustomComponents.DisablableButton("save variation", activeOpVariation.ActiveGroup != null))
        //     // {
        //     //     variationSystem.AppendPresetToCurrentGroup();
        //     // }
        //
        //     var topLeftCorner = ImGui.GetCursorScreenPos();
        //
        //     // Scene column mode
        //     ImGui.PushFont(Fonts.FontSmall);
        //     ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
        //     if (variationHandling.ActiveOperatorVariation.IsGroupExpanded)
        //     {
        //         var groupIndex = variationHandling.ActiveOperatorVariation.ActiveGroupIndex;
        //         var group = variationHandling.ActiveOperatorVariation.ActiveGroup;
        //         if (@group != null)
        //         {
        //             ImGui.PushID(groupIndex);
        //             ImGui.SetCursorScreenPos(topLeftCorner);
        //             ImGui.BeginGroup();
        //
        //             // Group column header
        //             ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
        //             ImGui.PushStyleColor(ImGuiCol.Text, _activeGroupColor.Rgba);
        //             ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Color(0.1f).Rgba);
        //             if (ImGui.Button($"{groupIndex}\n{@group.Title}", new Vector2(ColumnWidth, GroupHeaderHeight)))
        //             {
        //                 variationHandling.ActivateGroupAtIndex(groupIndex);
        //             }
        //
        //             ImGui.PopStyleColor(3);
        //
        //             for (var sceneIndex = 0; sceneIndex < GroupRows * GridColumns; sceneIndex++)
        //             {
        //                 DrawPresetToggle(sceneIndex, activeOpVariation, groupIndex, @group, variationHandling);
        //                 if (sceneIndex == 0 || (sceneIndex + 1) % GridColumns > 0)
        //                     ImGui.SameLine();
        //             }
        //
        //             ImGui.EndGroup();
        //
        //             DrawBlendOrTransitionSlider(activeOpVariation, @group, variationHandling);
        //
        //             ImGui.PopID();
        //         }
        //     }
        //     else
        //     {
        //         for (var groupIndex = 0; groupIndex < activeOpVariation.Groups.Count; groupIndex++)
        //         {
        //             ImGui.PushID(groupIndex);
        //             ImGui.SetCursorScreenPos(topLeftCorner + new Vector2((ColumnWidth + SliderWidth + GroupPadding) * groupIndex, 0));
        //             ImGui.BeginGroup();
        //
        //             var isActiveGroup = groupIndex == activeOpVariation.ActiveGroupIndex;
        //
        //             var group = activeOpVariation.Groups[groupIndex];
        //             if (@group == null)
        //                 continue;
        //
        //             // Group column header
        //             ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
        //             ImGui.PushStyleColor(ImGuiCol.Text, isActiveGroup ? _activeGroupColor : _mutedTextColor.Rgba);
        //             ImGui.PushStyleColor(ImGuiCol.ButtonHovered, isActiveGroup ? new Color(0.1f) : Color.Transparent.Rgba);
        //             if (ImGui.Button($"{groupIndex}\n{@group.Title}", new Vector2(ColumnWidth, GroupHeaderHeight)))
        //             {
        //                 variationHandling.ActivateGroupAtIndex(groupIndex);
        //             }
        //
        //             ImGui.PopStyleColor(3);
        //
        //             for (var sceneIndex = 0; sceneIndex < GroupRows; sceneIndex++)
        //             {
        //                 DrawPresetToggle(sceneIndex, activeOpVariation, groupIndex, @group, variationHandling);
        //             }
        //
        //             ImGui.EndGroup();
        //             DrawBlendOrTransitionSlider(activeOpVariation, @group, variationHandling);
        //
        //             ImGui.PopID();
        //         }
        //     }
        //
        //     ImGui.PopStyleVar();
        //     ImGui.PopFont();
        //
        //     // if (variationHandling.ActiveOperatorVariation.ActiveGroup != null && activeContext.ActiveGroup.BlendedPresets.Count > 1)
        //     // {
        //     //     if (DrawBlendSlider(ref _blendValue))
        //     //     {
        //     //         variationHandling.BlendGroupPresets(activeContext.ActiveGroup, _blendValue);
        //     //     }
        //     // }
        // }

        private static void DrawBlendOrTransitionSlider(OperatorVariation activeOpVariation, ParameterGroup group, LegacyVariationHandling presetSystem)
        {
            ImGui.SameLine();
            if (group.IsTransitionActive)
            {
                DrawBlendSlider(ref group.BlendTransitionProgress, activeOpVariation, group,
                                new List<LegacyPreset>() { group.BlendStartPreset, group.BlendTargetPreset });
            }
            else
            {
                if (DrawBlendSlider(ref _blendValue, activeOpVariation, group, group.BlendedPresets))
                    presetSystem.ActiveOperatorVariation?.BlendGroupPresets(activeOpVariation.ActiveGroup, _blendValue);
            }
        }

        private static void DrawPresetToggle(int sceneIndex, OperatorVariation activeOpVariation, int groupIndex, ParameterGroup group,
                                             LegacyVariationHandling presetSystem)
        {
            LegacyPreset preset = null;

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
                    case LegacyPreset.States.Active:
                        textColor = Color.White;
                        backgroundColor = _activePresetColor;
                        break;
                    case LegacyPreset.States.Modified:
                        textColor = Color.White;
                        backgroundColor = Color.Green;
                        break;
                    case LegacyPreset.States.IsBlended:
                        float f = group.BlendedPresets.IndexOf(preset) / (float)group.BlendedPresets.Count;
                        textColor = Color.White;

                        backgroundColor = f >= 0
                                              ? Color.Mix(_blendStartColor, _blendEndColor, f)
                                              : Color.Blue;
                        break;
                }

                ImGui.PushStyleColor(ImGuiCol.Button, backgroundColor.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, backgroundColor.Rgba);
                ImGui.PushStyleColor(ImGuiCol.Text, textColor.Rgba);

                if (_renamePresetReference == preset)
                {
                    var text = preset.Title;
                    ImGui.SetNextItemWidth(150);
                    ImGui.InputText("##input", ref text, 256);
                    ImGui.SetKeyboardFocusHere();
                    preset.Title = text;

                    if ((ImGui.IsItemDeactivated() || ImGui.IsKeyPressed((ImGuiKey)Key.Return) || ImGui.IsMouseClicked(ImGuiMouseButton.Left)))
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
                            preset.State = LegacyPreset.States.InActive;
                        }
                        else
                        {
                            if (group.BlendedPresets.Count == 0)
                            {
                                var lastActivePreset = group.ActivePreset;
                                presetSystem.ActiveOperatorVariation?.ActivatePreset(group, preset);

                                if (lastActivePreset != null)
                                {
                                    lastActivePreset.State = LegacyPreset.States.IsBlended;
                                    group.BlendedPresets.Add(lastActivePreset);
                                }
                            }

                            group.BlendedPresets.Add(preset);
                            preset.State = LegacyPreset.States.IsBlended;
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

                ImGui.PopStyleColor(3);
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

        private static LegacyPreset _renamePresetReference;
        private static float[] BlendDurations = new[] { 0, 0.25f, 0.5f, 1, 2, 4, 16, 32, 64, 128 };

        private static bool DrawBlendSlider(ref float value, OperatorVariation activeOpVariation, ParameterGroup group, List<LegacyPreset> presets)
        {
            var edited = false;
            ImGui.BeginGroup();
            {
                ImGui.TextUnformatted(GetDurationLabel(group.BlendTransitionDuration));
                ImGui.SetNextWindowPos(ImGui.GetItemRectMin());
                CustomComponents.ContextMenuForItem(() =>
                                                    {
                                                        foreach (var d in BlendDurations)
                                                        {
                                                            var seconds = d * 240 / Playback.Current.Bpm;
                                                            var isCurrent = Math.Abs(group.BlendTransitionDuration - d) < 0.01f;
                                                            
                                                            var label = GetDurationLabel(d);
                                                            
                                                            if (ImGui.MenuItem(label, $"({seconds}s)", isCurrent))
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

        private static string GetDurationLabel(float d)
        {
            var label = d == 0
                            ? "Instant"
                            : $"{d} bars";
            return label;
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