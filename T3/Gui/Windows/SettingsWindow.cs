using System.Collections.Generic;
using System.Threading.Channels;
using System.Windows.Forms;
using ImGuiNET;
using T3.Gui.Commands;
using T3.Gui.Graph;
using T3.Gui.TypeColors;
using T3.Gui.UiHelpers;
using System.Numerics;
using t3.Gui;

namespace T3.Gui.Windows
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            Config.Title = "Settings";
        }

        public static bool UseVSync = true;

        public static bool WindowRegionsVisible;
        public static bool ItemRegionsVisible;

        protected override void DrawContent()
        {
            var changed = false;
            ImGui.NewLine();
            if (ImGui.TreeNode("User Interface"))
            {
                changed |= SettingsUi.DrawSettingsTable("##uisettingstable", userInterfaceSettings);
                ImGui.TreePop();
            }
            
            if (ImGui.TreeNode("Space Mouse"))
            {
                changed |= SettingsUi.DrawSettingsTable("##settingspacemousetable", spaceMouseSettings);
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Additional settings"))
            {
                changed |= SettingsUi.DrawSettingsTable("##additionalsettingstable", additionalSettings);
                ImGui.TreePop();
            }

#if DEBUG
            if (ImGui.TreeNode("Debug Options"))
            {
                SettingsUi.DrawSettings(debugSettings);
                if (ImGui.TreeNode("Undo Queue"))
                {
                    ImGui.Indent();
                    ImGui.TextUnformatted("Undo");
                    ImGui.Indent();
                    foreach (var c in UndoRedoStack.UndoStack)
                    {
                        ImGui.Selectable(c.Name);
                    }

                    ImGui.Unindent();
                    ImGui.Spacing();
                    ImGui.TextUnformatted("Redo");
                    ImGui.Indent();
                    foreach (var c in UndoRedoStack.RedoStack)
                    {
                        ImGui.Selectable(c.Name);
                    }

                    ImGui.Unindent();
                    ImGui.Unindent();
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Modified Symbols"))
                {
                    foreach (var symbolUi in UiModel.GetModifiedSymbolUis())
                    {
                        if (symbolUi.HasBeenModified)
                        {
                            ImGui.TextUnformatted(symbolUi.Symbol.Namespace + ". " +  symbolUi.Symbol.Name);
                        }
                    }
                    
                    ImGui.TreePop();
                }                
                
                ImGui.TreePop();
            }
            
#endif
#if DEBUG
            if (ImGui.TreeNode("Look (not saved)"))
            {
                ColorVariations.DrawSettingsUi();

                if (ImGui.TreeNode("T3 Ui Style"))
                {
                    SettingsUi.DrawSettingsTable("##t3uistylesettings", t3UiStyleSettings);
                }
                
                if (ImGui.TreeNode("T3 Graph colors"))
                {
                    T3Style.DrawUi();
                    ImGui.TreePop();
                }                
                ImGui.TreePop();
            }
#endif

            if (changed)
                UserSettings.Save();

#if DEBUG
            ImGui.Separator();
            T3Metrics.Draw();
#endif
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }
    }
}