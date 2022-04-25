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
            // Mock implementation
            // if (ImGui.Button("Save Screenshot"))
            // {
            //     var outputWindow = OutputWindow.OutputWindowInstances.FirstOrDefault(w => w.Config.Visible);
            //     if (outputWindow is OutputWindow outWindow)
            //     {
            //         if (outWindow.ShownInstance.Outputs.Count > 0)
            //         {
            //             var outputSlot = outWindow.ShownInstance.Outputs[0];
            //             if (outputSlot is Slot<Texture2D> texture2dSlot)
            //             {
            //                 var texture = texture2dSlot.Value;
            //                 var srv = SrvManager.GetSrvForTexture(texture);
            //                 // D3DX11SaveTextureToFile()
            //                 // SharpDX.Direct3D9.Texture.ToFile(
            //                 //                                  renderSetup.D3DImageContainer.SharedTexture,
            //                 //                                  filePath,
            //                 //                                  SharpDX.Direct3D9.ImageFileFormat.Png);
            //             }
            //         } 
            //     }
            // }
            
            // Delete actions need be deferred to prevent collection modification during iteration
            if (_variationToBeDeletedNextFrame != null)
            {
                _poolWithVariationToBeDeleted.DeleteVariation(_variationToBeDeletedNextFrame);
                _variationToBeDeletedNextFrame = null;
            }
            
            
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
                            var newVariation = VariationHandling.ActivePoolForPresets.CreatePresetOfInstanceSymbol(instance);
                            _variationForRenaming = newVariation;
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
            if (_variationForRenaming == variation)
            {
                ImGui.SetKeyboardFocusHere();
                ImGui.InputText("##label", ref variation.Title, 256);
                if (ImGui.IsItemDeactivatedAfterEdit() && ImGui.IsItemDeactivated())
                {
                    _variationForRenaming = null;
                }
                return;
            }
            
            
            
            ImGui.PushID(variation.ActivationIndex);
            var setCorrectly = DoesPresetVariationMatch(variation, instance);

            var color = setCorrectly == MatchTypes.NoMatch
                            ? Color.Gray
                            : Color.White;
            
            ImGui.PushStyleColor(ImGuiCol.Text, color.Rgba);
            ImGui.Button(variation.ToString());
            ImGui.PopStyleColor();
            CustomComponents.ContextMenuForItem(() =>
                                                {
                                                    if (ImGui.MenuItem("Delete"))
                                                    {
                                                        _variationToBeDeletedNextFrame = variation;
                                                        _poolWithVariationToBeDeleted = VariationHandling.ActivePoolForPresets;
                                                    }

                                                    if (ImGui.MenuItem("Rename"))
                                                    {
                                                        _variationForRenaming = variation;
                                                    }
                                                });
            
            if (ImGui.IsItemActive())
            {
                if(  ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                {
                    var delta = ImGui.GetMouseDragDelta().X;
                    if (MathF.Abs(delta) > 0)
                    {
                        _blendStrength = delta * 0.01f;
                        VariationHandling.ActivePoolForPresets.UpdateBlendPreset(instance, variation.ActivationIndex, _blendStrength);
                    }
                }
            }
            else if (ImGui.IsItemDeactivated())
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
            else if (ImGui.IsItemHovered())
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

            if (ImGui.IsItemActivated())
            {
                _blendStrength = 0;
            }
            

            
            if (ImGui.IsItemDeactivated())
            {
                //_variationCanvas.ClearVariations();
            }

            if (setCorrectly == MatchTypes.PresetParamsMatch)
            {
                ImGui.SameLine();
                if (ImGui.Button("Reset others"))
                {
                    VariationHandling.ActivePoolForPresets.ApplyPreset(instance, variation.ActivationIndex, true);
                }
            }
            ImGui.PopID();
        }


        private static Variation _variationForRenaming;
        private static Variation _hoveredVariation;
        private static float _blendStrength = 1;
        private static Variation _variationToBeDeletedNextFrame;
        private static SymbolVariationPool _poolWithVariationToBeDeleted;
        

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