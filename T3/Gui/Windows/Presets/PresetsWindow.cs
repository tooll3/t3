using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Operator.Presets;
using T3.Gui.Selection;

namespace T3.Gui.Windows.Presets
{
    /// <summary>
    /// Renders the <see cref="PresetsWindow"/>
    /// </summary>
    public class PresetsWindow : Window
    {
        public PresetsWindow() : base()
        {
            Config.Title = "Presets";
            Config.Visible = true;
        }

        protected override void DrawContent()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.One * 5);
            {
                
                var selectedComposition = SelectionManager.GetSelectedComposition();
                if (selectedComposition == null)
                {
                    var instance = SelectionManager.GetSelectedInstance();
                    if (instance != null)
                        selectedComposition = instance.Parent;
                }
                
                if(selectedComposition == null) 
                {
                    CustomComponents.EmptyWindowMessage("Nothing selected");
                    return;
                }

                var presetLib = PresetRegistry.TryGetPresetLibForSymbol(selectedComposition.Symbol.Id);
                if (presetLib == null)
                {
                    CustomComponents.EmptyWindowMessage("No Presets saved for this \n" + selectedComposition.Symbol.Name);
                    return;
                }

                if (ImGui.Button("Add"))
                {
                    presetLib.AddNewPreset();
                }


                // List Presets
                foreach ( int index in presetLib.PresetsByIndex.Keys)
                {
                    ImGui.PushID(index);
                    if (ImGui.Button("" + index))
                    {
                        presetLib.ApplyPreset(selectedComposition, index);
                    }
                    ImGui.PopID();
                }
                
                var compositionUi = SymbolUiRegistry.Entries[selectedComposition.Symbol.Id];

                // List Ops and parameters 
                foreach (var reference in presetLib.ChildPresetReferences)
                {
                    ImGui.PushID(reference.SymbolChildId.GetHashCode());
                    if (T3Ui.HoveredIdsLastFrame.Contains(reference.SymbolChildId))
                    {
                        ImGui.Text("|");
                        ImGui.SameLine();
                    }

                    var childUi = compositionUi.ChildUis.Single(c => c.Id == reference.SymbolChildId);
                    
                    ImGui.Text(childUi.SymbolChild.ReadableName);
                    if (ImGui.IsItemHovered())
                    {
                        T3Ui.AddHoveredId(reference.SymbolChildId);
                    }
                    
                    if(ImGui.IsItemClicked(ImGuiMouseButton.Left))
                    {
                        var childInstance = selectedComposition.Children.Single(child => child.SymbolChildId == reference.SymbolChildId);
                        SelectionManager.SetSelection(childUi, childInstance);
                        FitViewToSelectionHandling.FitViewToSelection();
                    }
                    
                    foreach (var param in reference.DrivenInputs)
                    {
                        ImGui.PushID(param.Id.GetHashCode());
                        ImGui.TextColored(Color.Gray, "   ." + param.Name);
                        ImGui.SameLine();
                        foreach (var preset in presetLib.PresetsByIndex.Values)
                        {
                            ImGui.PushID(preset.Index);
                            
                            ImGui.Text(preset.Index == presetLib.CurrentPresetIndex ? "+" : ".");
                            if (ImGui.IsItemHovered())
                            {
                                var valueForPreset = preset.PresetValues.SingleOrDefault(pv => pv.InputId == param.Id);
                                if(valueForPreset != null)
                                    ImGui.SetTooltip(FormatValue(valueForPreset.InputValue));
                            }
                            ImGui.PopID();
                            ImGui.SameLine();
                        }
                        ImGui.PopID();
                        ImGui.NewLine();
                    }
                    ImGui.PopID();
                }
            }
            ImGui.PopStyleVar();
        }



        private static string FormatValue(InputValue value)
        {
            if (value is InputValue<float> floatValue)
            {
                return "" + floatValue.Value;
            }

            return "?";
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }
    }
}