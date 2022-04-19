using System.Collections.Generic;
using ImGuiNET;
using T3.Core.Logging;
using T3.Gui;
using t3.Gui.Interaction.Presets;
using T3.Gui.Windows;

namespace t3.Gui.Windows.Presets
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
            if (ImGui.BeginTabBar("##timeMode"))
            {
                if (ImGui.BeginTabItem("Presets"))
                {
                    var presetPool = PresetHandling.ActiveInstancePresetPool;
                    if (presetPool == null)
                    {
                        CustomComponents.EmptyWindowMessage("select one object for presets");
                    }
                    else
                    {
                        if (presetPool.Presets_.Count == 0)
                        {
                            CustomComponents.EmptyWindowMessage($"No Presets for {PresetHandling.ActiveInstanceForPresets.Symbol.Name}");
                        }
                        else {
                            var instance = PresetHandling.ActiveInstanceForPresets;
                            if (instance == null)
                            {
                                CustomComponents.EmptyWindowMessage($"NULL?!");    
                            }
                            else
                            {
                                foreach (var ttt in PresetHandling.ActiveInstancePresetPool.Presets_)
                                {
                                    //CustomComponents.EmptyWindowMessage($"No presets defined for {}");
                                    if (ImGui.Selectable(ttt.ToString()))
                                    {
                                        Log.Debug($"Activated {ttt}");
                                        PresetHandling.ActiveInstancePresetPool.ApplyPreset(instance, ttt.ActivationIndex);
                                    }
                                }
                            }
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