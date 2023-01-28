using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.Windows.Output;
using Icon = T3.Editor.Gui.Styling.Icon;

namespace T3.Editor.Gui.Windows
{
    /// <summary>
    /// A helper that decides which graph element to show.
    /// This is used by <see cref="OutputWindow"/> and eventually in <see cref="ParameterWindow"/>.
    /// </summary>
    public class ViewSelectionPinning
    {
        public void DrawPinning()
        {
            var pinnedOrSelectedInstance = GetPinnedOrSelectedInstance();
            if (pinnedOrSelectedInstance == null)
                return;

            FrameStats.AddPinnedId(pinnedOrSelectedInstance.SymbolChildId);    
            
            if (CustomComponents.IconButton(Icon.Pin, 
                                                   
                                                  new Vector2(T3Style.ToolBarHeight, T3Style.ToolBarHeight) * T3Ui.UiScaleFactor,
                                            _isPinned ? CustomComponents.ButtonStates.Activated : CustomComponents.ButtonStates.Normal
                                                  ))
            {
                _isPinned = !_isPinned;
                if (_isPinned)
                    PinSelectionToView();
            }

            
            
            ImGui.SameLine();
            ImGui.SetNextItemWidth(300);
            var suffix = _isPinned ? " (pinned)" : " (selected)";

            var pinnedEvaluationInstance = GetPinnedEvaluationInstance();
            if (pinnedEvaluationInstance != null)
            {
                suffix += " -> " + pinnedEvaluationInstance.Symbol.Name + " (Final)";
            }

            if (ImGui.BeginCombo("##pinning", pinnedOrSelectedInstance.Symbol.Name + suffix))
            {
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(6, 6));
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 6));
                if (_isPinned)
                {
                    if (ImGui.MenuItem("Unpin view"))
                    {
                        _isPinned = false;
                    }

                    var instanceSelectedInGraph = NodeSelection.GetFirstSelectedInstance();
                    if (instanceSelectedInGraph != pinnedOrSelectedInstance)
                    {
                        // var selectionIsPartOfTree = instanceSelectedInGraph.Outputs[0].DirtyFlag.FramesSinceLastUpdate < 2;
                        // if (selectionIsPartOfTree)
                        // {
                        //     if (instanceSelectedInGraph == GetPinnedEvaluationInstance())
                        //     {
                        //         if (ImGui.MenuItem("Unpin Selected Rendering Step"))
                        //         {
                        //             //PinEvaluationToSelection();
                        //         }
                        //     }
                        //     else
                        //     {
                        //         if (ImGui.MenuItem("Pin Selected Rendering Step"))
                        //         {
                        //             PinSelectionToView();
                        //             PinSelectionAsEvaluationStart(pinnedOrSelectedInstance);
                        //         }
                        //     }
                        // }
                        // else
                        // {
                        if (ImGui.MenuItem("Pin Selection to View"))
                        {
                            _isPinned = true;
                            PinSelectionToView();
                        }

                        // }
                    }
                }
                else
                {
                    if (ImGui.MenuItem("Pin Selection to View"))
                    {
                        _isPinned = true;
                        PinSelectionToView();
                    }
                }

                if (pinnedEvaluationInstance != null)
                {
                    if (ImGui.MenuItem("Unpin start operator"))
                    {
                        _pinnedEvaluationInstancePath = null;
                    }
                }
                else
                {
                    if (ImGui.MenuItem("Pin as start operator"))
                    {
                        PinSelectionAsEvaluationStart(NodeSelection.GetFirstSelectedInstance());
                    }
                }

                if (GraphCanvas.Current?.CompositionOp != null)
                {
                    if (ImGui.MenuItem("Show in Graph"))
                    {
                        var parentInstance = pinnedOrSelectedInstance.Parent;
                        var parentSymbolUi = SymbolUiRegistry.Entries[parentInstance.Symbol.Id];
                        var instanceChildUi = parentSymbolUi.ChildUis.Single(childUi => childUi.Id == pinnedOrSelectedInstance.SymbolChildId);
                        NodeSelection.SetSelectionToChildUi(instanceChildUi, pinnedOrSelectedInstance);
                        FitViewToSelectionHandling.FitViewToSelection();
                    }
                }

                if (pinnedOrSelectedInstance.Outputs.Count > 1)
                {
                    if (ImGui.BeginMenu("Show Output..."))
                    {
                        foreach (var output in pinnedOrSelectedInstance.Outputs)
                        {
                            ImGui.MenuItem(output.ToString());
                        }

                        ImGui.EndMenu();
                    }
                }

                ImGui.Separator();
                ImGui.MenuItem("Show hovered outputs", false);
                ImGui.PopStyleVar(2);
                ImGui.EndCombo();
            }

            ImGui.SameLine();
        }

        private void PinSelectionToView()
        {
            var firstSelectedInstance = NodeSelection.GetFirstSelectedInstance();
            PinInstance(firstSelectedInstance);
            //_pinnedEvaluationInstancePath = null;
        }

        public void PinInstance(Instance instance)
        {
            _pinnedInstancePath = OperatorUtils.BuildIdPathForInstance(instance);
            _isPinned = true;
        }

        private void PinSelectionAsEvaluationStart(Instance instance)
        {
            _pinnedEvaluationInstancePath = OperatorUtils.BuildIdPathForInstance(instance);
        }

        public Instance GetPinnedOrSelectedInstance()
        {
            if (!_isPinned)
                return NodeSelection.GetFirstSelectedInstance();

            var instance = NodeOperations.GetInstanceFromIdPath(_pinnedInstancePath);
            return instance ?? NodeSelection.GetFirstSelectedInstance();
        }

        public Instance GetPinnedEvaluationInstance()
        {
            return NodeOperations.GetInstanceFromIdPath(_pinnedEvaluationInstancePath);
        }

        private bool _isPinned;
        private List<Guid> _pinnedInstancePath = new List<Guid>();
        private List<Guid> _pinnedEvaluationInstancePath = new List<Guid>();
    }
}