using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using T3.Core;
using T3.Core.Logging;
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

            if (activeContext.ActiveGroup != null)
            {
                if (ImGui.Button("save preset"))
                {
                    presetSystem.AppendPresetToCurrentGroup();
                }
            }

            var topLeftCorner = ImGui.GetCursorScreenPos();

            const float columnWidth = 50;
            const float gridRows = 16;
            const float gridRowHeight = 14;

            // Scene grid mode
            ImGui.PushFont(Fonts.FontSmall);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
            for (var groupIndex = 0; groupIndex < activeContext.Groups.Count; groupIndex++)
            {
                ImGui.PushID(groupIndex);
                ImGui.SetCursorScreenPos(topLeftCorner + new Vector2((columnWidth + 1) * groupIndex, 0));
                ImGui.BeginGroup();

                var isActiveGroup = groupIndex == activeContext.ActiveGroupIndex;

                var group = activeContext.Groups[groupIndex];
                if (group == null)
                    continue;

                // Group column header
                ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
                ImGui.PushStyleColor(ImGuiCol.Text, isActiveGroup ? _activeGroupColor : _mutedTextColor.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, isActiveGroup ? new Color(0.1f) : Color.Transparent.Rgba);
                if (ImGui.Button($"{groupIndex}\n{group.Title}", new Vector2(columnWidth, 40)))
                {
                    presetSystem.ActivateGroupAtIndex(groupIndex);
                }

                ImGui.PopStyleColor(3);

                for (var sceneIndex = 0; sceneIndex < gridRows; sceneIndex++)
                {
                    Preset preset = null;
                    
                    if(sceneIndex < activeContext.Presets.GetLength(1) )
                        preset = activeContext.Presets[groupIndex, sceneIndex];
                    
                    ImGui.PushID(sceneIndex);
                    var title = preset == null
                                    ? "-"
                                    : $"{sceneIndex}"; // string.IsNullOrEmpty(preset.Name)

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
                                textColor = Color.White;
                                backgroundColor = Color.Blue;
                                break;
                        }

                        ImGui.PushStyleColor(ImGuiCol.Button, backgroundColor.Rgba);
                        ImGui.PushStyleColor(ImGuiCol.Text, textColor.Rgba);

                        if (ImGui.Button(title, new Vector2(columnWidth, gridRowHeight)))
                        {
                            if (ImGui.GetIO().KeyCtrl)
                            {
                                presetSystem.RemovePresetAtAddress(new PresetAddress(groupIndex, sceneIndex));
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
                                        presetSystem.ActivatePreset(group, preset);
                                        
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
                                presetSystem.ActivatePreset(group, preset);
                            }
                        }

                        ImGui.PopStyleColor(2);
                    }
                    else
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, _emptySlotColor.Rgba);
                        if (ImGui.Button("+", new Vector2(columnWidth, gridRowHeight)))
                        {
                            presetSystem.CreatePresetAtAddress(new PresetAddress(groupIndex, sceneIndex));
                        }

                        ImGui.PopStyleColor();
                    }

                    ImGui.PopID();
                }

                ImGui.EndGroup();
                ImGui.SameLine();
                ImGui.PopID();
            }
            ImGui.PopStyleVar();
            ImGui.PopFont();

            if (presetSystem.ActiveContext.ActiveGroup != null && activeContext.ActiveGroup.BlendedPresets.Count > 1)
            {
                if (DrawBlendSlider(ref _blendValue))
                {
                    presetSystem.BlendGroupPresets(activeContext.ActiveGroup, _blendValue);
                }
            }
        }

        private static float _blendValue;

        private static bool DrawBlendSlider(ref float value)
        {
            const float sliderHeight = 200;
            const float sliderWidth = 50;
            var buttonSize = new Vector2(sliderWidth, sliderHeight);
            ImGui.Button(".", buttonSize);

            var edited = false;
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                ImGui.GetID(string.Empty);
                _previousValue = value;
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
            }

            var p = ImGui.GetItemRectMin() + new Vector2(0, sliderHeight * value - 1);
            ImGui.GetWindowDrawList().AddRectFilled(p, p + new Vector2(sliderWidth, 2), Color.Orange);

            var isDragging = ImGui.IsMouseDragging(ImGuiMouseButton.Left) && ImGui.IsItemActive();
            if (isDragging)
            {
                value = (_previousValue + ImGui.GetMouseDragDelta().Y / sliderHeight).Clamp(0, 1);
                edited = true;
            }

            return edited;
        }

        private static float _previousValue;

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }

        private static readonly Color _activeGroupColor = Color.White;
        private static readonly Color _mutedTextColor = Color.Gray;
        private static readonly Color _activePresetColor = Color.Orange;
        private static readonly Color _emptySlotColor = Color.Black;
        private static readonly Color _presetColor = new Color(0.2f);
    }
}