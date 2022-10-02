using System.Collections.Generic;
using System.Threading.Channels;
using System.Windows.Forms;
using ImGuiNET;
using T3.Gui.Commands;
using T3.Gui.Graph;
using T3.Gui.TypeColors;
using T3.Gui.UiHelpers;
using System.Numerics;

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
                changed |= DrawSettingsTable("##UserInterfaceTable", userInterfaceSettings);
                ImGui.TreePop();
            }
            
            if (ImGui.TreeNode("Space Mouse"))
            {
                changed |= DrawSettingsTable("##SpaceMouseTable", spaceMouseSettings);
                ImGui.TreePop();
            }

            
            if (ImGui.TreeNode("Additional settings"))
            {
                changed |= DrawSettingsTable("##AdditionalSettings", additionalSettings);
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Debug Options"))
            {
                ImGui.Checkbox("VSync", ref UseVSync);
                ImGui.Checkbox("Show Window Regions", ref WindowRegionsVisible);
                ImGui.Checkbox("Show Item Regions", ref ItemRegionsVisible);

                if (ImGui.TreeNode("Undo Queue"))
                {
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

            if (ImGui.TreeNode("Look (not saved)"))
            {
                ColorVariations.DrawSettingsUi();

                if (ImGui.TreeNode("T3 Ui Style"))
                {
                    ImGui.DragFloat("Height Connection Zone", ref GraphNode.UsableSlotThickness);
                    ImGui.DragFloat2("Label position", ref GraphNode.LabelPos);
                    ImGui.DragFloat("Slot Gaps", ref GraphNode.SlotGaps, 0.1f, 0, 10f);
                    ImGui.DragFloat("Input Slot Margin Y", ref GraphNode.InputSlotMargin, 0.1f, 0, 10f);
                    ImGui.DragFloat("Input Slot Thickness", ref GraphNode.InputSlotThickness, 0.1f, 0, 10f);
                    ImGui.DragFloat("Output Slot Margin", ref GraphNode.OutputSlotMargin, 0.1f, 0, 10f);

                    ImGui.ColorEdit4("ValueLabelColor", ref T3Style.Colors.ValueLabelColor.Rgba);
                    ImGui.ColorEdit4("ValueLabelColorHover", ref T3Style.Colors.ValueLabelColorHover.Rgba);
                }
                
                if (ImGui.TreeNode("T3 Graph colors"))
                {
                    T3Style.DrawUi();
                    ImGui.TreePop();
                }                
                ImGui.TreePop();
            }
            
            if(changed)
                UserSettings.Save();
            
            ImGui.Separator();
            T3Metrics.Draw();
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }

        bool DrawSettingsTable(string tableID, UIControlledSetting[] settings)
        {
            ImGui.NewLine();
            bool changed = false;
            if (ImGui.BeginTable(tableID, 2, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingFixedSame | ImGuiTableFlags.PadOuterX))
            {
                foreach (UIControlledSetting setting in settings)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Indent();
                    ImGui.Text(setting.label);
                    ImGui.Unindent();

                    if (!string.IsNullOrEmpty(setting.tooltip))
                    {
                        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                        {
                            ImGui.SetTooltip(setting.tooltip);
                        }
                    }

                    ImGui.TableNextColumn();

                    bool valueChanged = setting.imguiFunc.Invoke();
                    if(valueChanged && setting.OnValueChanged != null)
                    {
                        setting.OnValueChanged.Invoke();
                    }

                    changed |= valueChanged;
                }
            }

            ImGui.EndTable();
            ImGui.NewLine();

            return changed;
        }
    }
}