using System;
using System.Collections.Generic;
using System.Numerics;
using Core.Resource;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Interaction.Variations;
using T3.Gui.Interaction.Variations.Model;
using T3.Gui.OutputUi;
using T3.Gui.Selection;
using T3.Gui.Styling;

namespace T3.Gui.Windows.Variations
{
    public class VariationsWindow : Window
    {
        public VariationsWindow()
        {
            _variationCanvas = new VariationCanvas(this);
            Config.Title = "Variations";
            WindowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        }

        protected override void DrawContent()
        {
            DrawWindowContent();
        }

        private void DrawWindowContent()
        {
            if (VariationHandling.ActiveInstanceForPresets == null || VariationHandling.ActivePoolForPresets == null)
            {
                return;
            }

            if (VariationHandling.ActivePoolForPresets.Variations.Count == 0)
            {
                CustomComponents.EmptyWindowMessage("No presets yet");
            }
            
            // Delete actions need be deferred to prevent collection modification during iteration
            if (_variationsToBeDeletedNextFrame.Count > 0)
            {
                _poolWithVariationToBeDeleted.DeleteVariations(_variationsToBeDeletedNextFrame);
                _variationsToBeDeletedNextFrame.Clear();
            }

            var drawList = ImGui.GetWindowDrawList();
            var keepCursorPos = ImGui.GetCursorScreenPos();

            drawList.ChannelsSplit(2);
            drawList.ChannelsSetCurrent(1);
            {
                ImGui.BeginChild("header", new Vector2(20, 20));
                if (CustomComponents.IconButton(Icon.Plus, "## addbutton", new Vector2(20, 20)))
                {
                    var newVariation = VariationHandling.ActivePoolForPresets.CreatePresetOfInstanceSymbol(VariationHandling.ActiveInstanceForPresets);
                    _variationForRenaming = newVariation;
                }

                ImGui.EndChild();
            }

            drawList.ChannelsSetCurrent(0);
            {
                ImGui.SetCursorScreenPos(keepCursorPos);

                if (VariationHandling.ActivePoolForPresets != null)
                {
                    _variationCanvas.Draw(drawList, VariationHandling.ActivePoolForPresets);
                }
            }

            drawList.ChannelsMerge();

            // if (ImGui.BeginTabBar("##presets"))
            // {
            //     if (ImGui.BeginTabItem("Presets"))
            //     {
            //         var presetPool = VariationHandling.ActivePoolForPresets;
            //         if (presetPool == null)
            //         {
            //             CustomComponents.EmptyWindowMessage("select one object for presets");
            //         }
            //         else
            //         {
            //             var instance = VariationHandling.ActiveInstanceForPresets;
            //             if (presetPool.Variations.Count == 0)
            //             {
            //                 CustomComponents.EmptyWindowMessage($"No Presets for {VariationHandling.ActiveInstanceForPresets.Symbol.Name}");
            //             }
            //             else
            //             {
            //                 if (instance == null)
            //                 {
            //                     CustomComponents.EmptyWindowMessage($"NULL?!");
            //                 }
            //                 else
            //                 {
            //                     foreach (var variation in VariationHandling.ActivePoolForPresets.Variations)
            //                     {
            //                         DrawPresetButton(variation, instance);
            //                     }
            //                 }
            //             }
            //
            //             if (ImGui.Button("Create"))
            //             {
            //                 var newVariation = VariationHandling.ActivePoolForPresets.CreatePresetOfInstanceSymbol(instance);
            //                 _variationForRenaming = newVariation;
            //             }
            //         }
            //
            //         ImGui.EndTabItem();
            //     }
            //
            //     if (ImGui.BeginTabItem("Variations"))
            //     {
            //         CustomComponents.EmptyWindowMessage("This comes later");
            //         ImGui.EndTabItem();
            //     }
            // }
            //
            // ImGui.EndTabBar();
        }

        // private static void DrawPresetButton(Variation variation, Instance instance)
        // {
        //     if (_variationForRenaming == variation)
        //     {
        //         ImGui.PushID(variation.ActivationIndex);
        //         ImGui.SetKeyboardFocusHere();
        //         ImGui.InputText("##label", ref variation.Title, 256);
        //
        //         if (ImGui.IsItemDeactivatedAfterEdit() && ImGui.IsItemDeactivated())
        //         {
        //             _variationForRenaming = null;
        //         }
        //
        //         ImGui.PopID();
        //
        //         return;
        //     }
        //
        //     ImGui.PushID(variation.Id.GetHashCode());
        //     var setCorrectly = DoesPresetVariationMatch(variation, instance);
        //
        //     var color = setCorrectly == MatchTypes.NoMatch
        //                     ? Color.Gray
        //                     : Color.White;
        //
        //     ImGui.PushStyleColor(ImGuiCol.Text, color.Rgba);
        //     var sliderWidth = (int)MathF.Min(100, ImGui.GetContentRegionAvail().X * 0.3f);
        //     ImGui.Button("##empty", new Vector2(-30, 0));
        //     ImGui.PopStyleColor();
        //     CustomComponents.ContextMenuForItem(() =>
        //                                         {
        //                                             if (ImGui.MenuItem("Delete"))
        //                                             {
        //                                                 _variationsToBeDeletedNextFrame.Add(variation);
        //                                                 _poolWithVariationToBeDeleted = VariationHandling.ActivePoolForPresets;
        //                                             }
        //
        //                                             if (ImGui.MenuItem("Rename"))
        //                                             {
        //                                                 _variationForRenaming = variation;
        //                                             }
        //                                         });
        //
        //     if (ImGui.IsItemActive())
        //     {
        //         if (_hoveredVariation != variation)
        //         {
        //             if (_hoveredVariation != null)
        //                 VariationHandling.ActivePoolForPresets.StopHover();
        //
        //             VariationHandling.ActivePoolForPresets.BeginHoverPreset(instance, variation);
        //         }
        //
        //         if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        //         {
        //             var delta = ImGui.GetMouseDragDelta().X;
        //             if (MathF.Abs(delta) > 0)
        //             {
        //                 _blendStrength = delta * 0.01f;
        //                 VariationHandling.ActivePoolForPresets.UpdateBlendPreset(instance, variation, _blendStrength);
        //             }
        //         }
        //     }
        //     else if (ImGui.IsItemDeactivated())
        //     {
        //         if (_hoveredVariation != null)
        //         {
        //             VariationHandling.ActivePoolForPresets.ApplyHovered();
        //         }
        //         else
        //         {
        //             Log.Warning("Clicked without hovering variation button first?");
        //         }
        //
        //         _hoveredVariation = null;
        //     }
        //     else if (ImGui.IsItemHovered())
        //     {
        //         var posInItem = ImGui.GetMousePos() - ImGui.GetItemRectMin();
        //         var sliderFactor = 1 - MathF.Max(0, MathF.Min(1, posInItem.X / sliderWidth));
        //
        //         if (_hoveredVariation != variation)
        //         {
        //             if (_hoveredVariation != null)
        //             {
        //                 VariationHandling.ActivePoolForPresets.StopHover();
        //             }
        //
        //             VariationHandling.ActivePoolForPresets.BeginHoverPreset(instance, variation);
        //             _hoveredVariation = variation;
        //         }
        //
        //         VariationHandling.ActivePoolForPresets.UpdateBlendPreset(instance, variation, sliderFactor);
        //     }
        //     else if (_hoveredVariation == variation)
        //     {
        //         VariationHandling.ActivePoolForPresets.StopHover();
        //         _hoveredVariation = null;
        //     }
        //
        //     if (ImGui.IsItemActivated())
        //     {
        //         _blendStrength = 0;
        //     }
        //
        //     // Draw type indicators
        //     var lastItemSize = ImGui.GetItemRectSize();
        //     var drawList = ImGui.GetWindowDrawList();
        //     var itemRectMin = ImGui.GetItemRectMin();
        //
        //     drawList.PushClipRect(itemRectMin, ImGui.GetItemRectMax(), true);
        //
        //     var position = itemRectMin + new Vector2(lastItemSize.X - _valueMatching.Count * 4 - 4, 0);
        //     for (var index = 0; index < _valueMatching.Count; index++)
        //     {
        //         var matching = _valueMatching[index];
        //         var slotTypeIcon = _valueSlotTypes[index];
        //         Icons.DrawIconAtScreenPosition(slotTypeIcon, position, drawList, _matchColors[(int)matching]);
        //         position += new Vector2(4, 0);
        //     }
        //
        //     // Draw Blend region indicator
        //     drawList.AddTriangleFilled(itemRectMin,
        //                                itemRectMin + new Vector2(sliderWidth, 0),
        //                                itemRectMin + new Vector2(0, lastItemSize.Y), Color.Black.Fade(0.2f)
        //                               );
        //
        //     // Print label
        //     drawList.AddText(itemRectMin + new Vector2(sliderWidth + 4, 3),
        //                      Color.Gray,
        //                      variation.ToString());
        //
        //     // Reset non defaults button
        //     if (setCorrectly == MatchTypes.PresetParamsMatch)
        //     {
        //         ImGui.SameLine();
        //         if (ImGui.Button("Reset others"))
        //         {
        //             VariationHandling.ActivePoolForPresets.ApplyPreset(instance, variation, true);
        //         }
        //     }
        //
        //     ImGui.PopClipRect();
        //
        //     ImGui.PopID();
        // }

        private static Variation _variationForRenaming;
        //private static Variation _hoveredVariation;
        // private static float _blendStrength = 1;
        private static readonly List<Variation> _variationsToBeDeletedNextFrame = new(20);
        private static SymbolVariationPool _poolWithVariationToBeDeleted;
        //
        private static readonly List<Icon> _valueSlotTypes = new(20);
        private static readonly List<ValueMatches> _valueMatching = new(20);

        // must match length for MatchTypes
        private static readonly Color[] _matchColors = new[]
                                                           {
                                                               Color.Gray,
                                                               Color.Gray,
                                                               Color.Black,
                                                               Color.Black,
                                                           };

        private readonly VariationCanvas _variationCanvas;
        public IOutputUi OutputUi;

        private static MatchTypes DoesPresetVariationMatch(Variation variation, Instance instance)
        {
            var setCorrectly = true;
            var foundOneMatch = false;
            var foundUnknownNonDefaults = false;
            _valueMatching.Clear();
            _valueSlotTypes.Clear();

            foreach (var (symbolChildId, values) in variation.InputValuesForChildIds)
            {
                if (symbolChildId != Guid.Empty)
                    continue;

                foreach (var input in instance.Inputs)
                {
                    var inputIsDefault = input.Input.IsDefault;
                    var variationIncludesInput = values.ContainsKey(input.Id);

                    if (!ValueUtils.CompareFunctions.ContainsKey(input.ValueType))
                        continue;

                    Icon icon = Icon.SlotFloat;

                    if (input.ValueType == typeof(float))
                    {
                        icon = Icon.SlotFloat;
                    }
                    else if (input.ValueType == typeof(Vector2))
                    {
                        icon = Icon.SlotVector2;
                    }
                    else if (input.ValueType == typeof(Vector3))
                    {
                        icon = Icon.SlotVector3;
                    }
                    else if (input.ValueType == typeof(Vector4))
                    {
                        icon = Icon.SlotColor;
                    }

                    _valueSlotTypes.Add(icon);

                    ValueMatches matching;
                    if (variationIncludesInput)
                    {
                        foundOneMatch = true;

                        if (inputIsDefault)
                        {
                            matching = ValueMatches.NotEqual;
                            setCorrectly = false;
                        }
                        else
                        {
                            var inputValueMatches = ValueUtils.CompareFunctions[input.ValueType](values[input.Id], input.Input.Value);
                            matching = inputValueMatches ? ValueMatches.Equal : ValueMatches.NotEqual;
                            setCorrectly &= inputValueMatches;
                        }
                    }
                    else
                    {
                        if (inputIsDefault)
                        {
                            matching = ValueMatches.IgnoredDefault;
                        }
                        else
                        {
                            matching = ValueMatches.IgnoredUndefinedNonDefault;
                            foundUnknownNonDefaults = true;
                        }
                    }

                    _valueMatching.Add(matching);
                }
            }

            if (!foundOneMatch || !setCorrectly)
            {
                return MatchTypes.NoMatch;
            }

            if (foundUnknownNonDefaults)
            {
                return MatchTypes.PresetParamsMatch;
            }

            return MatchTypes.PresetAndDefaultParamsMatch;
        }

        private enum MatchTypes
        {
            NoMatch,
            PresetParamsMatch,
            PresetAndDefaultParamsMatch,
        }

        private enum ValueMatches
        {
            NotEqual,
            Equal,
            IgnoredDefault,
            IgnoredUndefinedNonDefault,
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }

        public void DeleteVariations(List<Variation> selectionSelection)
        {
            _poolWithVariationToBeDeleted = VariationHandling.ActivePoolForPresets;
            _variationsToBeDeletedNextFrame.AddRange(selectionSelection);
        }
    }
}