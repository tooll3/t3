using System.Collections.Generic;
using ImGuiNET;
using T3.Core.Logging;
using T3.Gui;
using t3.Gui.Interaction.Presets;
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
                    var presetPool = VariationHandling.ActiveInstancePresetPool;
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
                                foreach (var ttt in VariationHandling.ActiveInstancePresetPool.Variations)
                                {
                                    //CustomComponents.EmptyWindowMessage($"No presets defined for {}");
                                    if (ImGui.Selectable(ttt.ToString()))
                                    {
                                        Log.Debug($"Activated {ttt}");
                                        VariationHandling.ActiveInstancePresetPool.ApplyPreset(instance, ttt.ActivationIndex);
                                    }
                                }
                            }
                        }
                        if (ImGui.Button("Create"))
                        {
                            VariationHandling.ActiveInstancePresetPool.CreatePresetOfInstanceSymbol(instance);
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


        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }
    }
}