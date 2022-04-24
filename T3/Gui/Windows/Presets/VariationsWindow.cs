using System;
using System.Collections.Generic;
using Core.Resource;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui;
using t3.Gui.Interaction.Variations;
using t3.Gui.Interaction.Variations.Model;
using T3.Gui.Windows;

namespace t3.Gui.Windows.Presets
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
            if (ImGui.BeginTabBar("##presets"))
            {
                if (ImGui.BeginTabItem("Presets"))
                {
                    var presetPool = VariationHandling.ActivePoolForPresets;
                    if (presetPool == null)
                    {
                        CustomComponents.EmptyWindowMessage("select one object for presets");
                    }
                    else
                    {
                        var instance = VariationHandling.ActiveInstanceForPresets;
                        if (presetPool.Variations.Count == 0)
                        {
                            CustomComponents.EmptyWindowMessage($"No Presets for {VariationHandling.ActiveInstanceForPresets.Symbol.Name}");
                        }
                        else {
                            if (instance == null)
                            {
                                CustomComponents.EmptyWindowMessage($"NULL?!");    
                            }
                            else
                            {
                                foreach (var variation in VariationHandling.ActivePoolForPresets.Variations)
                                {
                                    DrawPresetButton(variation, instance);
                                }
                            }
                        }
                        if (ImGui.Button("Create"))
                        {
                            VariationHandling.ActivePoolForPresets.CreatePresetOfInstanceSymbol(instance);
                        }
                    }


                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Variations"))
                {
                    CustomComponents.EmptyWindowMessage("This comes later");
                    ImGui.EndTabItem();
                }
            }

            ImGui.EndTabBar();

        }

        private static void DrawPresetButton(Variation variation, Instance instance)
        {
            var setCorrectly = DoesPresetVariationMatch(variation, instance);

            var s = "";
            switch (setCorrectly)
            {
                case MatchTypes.NoMatch:
                    break;
                case MatchTypes.PresetParamsMatch:
                    s = "<< Has other changes";
                    break;
                case MatchTypes.PresetAndDefaultParamsMatch:
                    s = "<<<";
                    break;
            }
            
            ImGui.Selectable(variation.ToString() + s);
            
            if (ImGui.IsItemActivated())
            {
                if (_hoveredVariation != null)
                {
                    VariationHandling.ActivePoolForPresets.ApplyHovered();
                }
                else
                {
                    Log.Warning("Clicked without hovering variation button first?");
                }

                _hoveredVariation = null;
            }
            
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
                
                if (_hoveredVariation != variation)
                {
                    if (_hoveredVariation != null)
                    {
                        VariationHandling.ActivePoolForPresets.StopHover();
                    }
                    
                    VariationHandling.ActivePoolForPresets.BeginHoverPreset(instance, variation.ActivationIndex);
                    _hoveredVariation = variation;
                }
            }
            else if(_hoveredVariation == variation)
            {
                VariationHandling.ActivePoolForPresets.StopHover();
                _hoveredVariation = null;
            }
            
            // if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            // {
            //     matchingParam.ScatterStrength = (_strengthBeforeDrag + ImGui.GetMouseDragDelta().X * 0.02f).Clamp(0, 100f);
            // }
            // if (ImGui.IsItemDeactivated())
            // {
            //     _variationCanvas.ClearVariations();
            // }
        }

        private static Variation _hoveredVariation;
        

        private static MatchTypes DoesPresetVariationMatch(Variation variation, Instance instance)
        {
            var setCorrectly = true;
            var foundOneMatch = false;
            var foundUnknownNonDefaults = false;
            
            foreach (var (symbolChildId, values) in variation.InputValuesForChildIds)
            {
                if (symbolChildId != Guid.Empty)
                    continue;

                foreach (var input in instance.Inputs)
                {
                    var inputIsDefault = input.Input.IsDefault;
                    var variationIncludesInput = values.ContainsKey(input.Id);
                    
                    if (!variationIncludesInput)
                    {
                        if (!inputIsDefault)
                        {
                            foundUnknownNonDefaults = true;
                        }                        
                        continue;
                    }

                    foundOneMatch = true;

                    if (inputIsDefault)
                    {
                        setCorrectly = false;
                    }
                    else
                    {
                        var inputValueMatches = ValueUtils.CompareFunctions[input.ValueType](values[input.Id], input.Input.Value);
                        setCorrectly &= inputValueMatches;
                    }
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

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }
    }
}