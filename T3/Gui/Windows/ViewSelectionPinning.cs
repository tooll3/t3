using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Operator;
using T3.Gui.Graph;
using T3.Gui.Graph.Interaction;
using T3.Gui.Selection;
using T3.Gui.Windows.Output;
using Icon = T3.Gui.Styling.Icon;

namespace T3.Gui.Windows
{
    /// <summary>
    /// A helper that decides with graph element to show.
    /// This is used by <see cref="OutputWindow"/> and eventually in <see cref="ParameterWindow"/>.
    /// </summary>
    public class ViewSelectionPinning
    {
        public void DrawPinning()
        {
            var pinnedOrSelectedInstance = GetPinnedOrSelectedInstance();
            if (pinnedOrSelectedInstance == null)
                return;

            if (CustomComponents.ToggleButton(Icon.Pin, "##pin", ref _isPinned, new Vector2(T3Style.ToolBarHeight, T3Style.ToolBarHeight)))
            {
                if (_isPinned)
                    SetPinningToSelection();
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(250);
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

                    var instanceSelectedInGraph = SelectionManager.GetSelectedInstance();
                    if (instanceSelectedInGraph != pinnedOrSelectedInstance)
                    {
                        var selectionIsPartOfTree = instanceSelectedInGraph.Outputs[0].DirtyFlag.FramesSinceLastUpdate < 2;
                        if (selectionIsPartOfTree)
                        {
                            if (instanceSelectedInGraph == GetPinnedEvaluationInstance())
                            {
                                if (ImGui.MenuItem("Unpin Selected Rendering Step"))
                                {
                                    PinEvaluationToSelection();
                                }
                            }
                            else
                            {
                                if (ImGui.MenuItem("Pin Selected Rendering Step"))
                                {
                                    PinEvaluationToSelection();
                                }
                            }
                        }
                        else
                        {
                            if (ImGui.MenuItem("Pin Selection to View"))
                            {
                                _isPinned = true;
                                SetPinningToSelection();
                            }
                        }
                    }
                }
                else
                {
                    if (ImGui.MenuItem("Pin Selection to View"))
                    {
                        _isPinned = true;
                        SetPinningToSelection();
                    }
                }

                if (GraphCanvas.Current.CompositionOp != null)
                {
                    if (ImGui.MenuItem("Show in Graph"))
                    {
                        var parentInstance = pinnedOrSelectedInstance.Parent;
                        var parentSymbolUi = SymbolUiRegistry.Entries[parentInstance.Symbol.Id];
                        var instanceChildUi = parentSymbolUi.ChildUis.Single(childUi => childUi.Id == pinnedOrSelectedInstance.SymbolChildId);
                        SelectionManager.SetSelection(instanceChildUi, pinnedOrSelectedInstance);
                        SelectionManager.FitViewToSelection();
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

        private void SetPinningToSelection()
        {
            _pinnedInstancePath = NodeOperations.BuildIdPathForInstance(SelectionManager.GetSelectedInstance());
            _pinnedEvaluationInstancePath = null;
        }

        private void PinEvaluationToSelection()
        {
            _pinnedEvaluationInstancePath = NodeOperations.BuildIdPathForInstance(SelectionManager.GetSelectedInstance());
        }

        public Instance GetPinnedOrSelectedInstance()
        {
            if (!_isPinned)
                return SelectionManager.GetSelectedInstance();

            var instance = NodeOperations.GetInstanceFromIdPath(_pinnedInstancePath);
            return instance ?? SelectionManager.GetSelectedInstance();
        }

        private bool _isPinned;
        private List<Guid> _pinnedInstancePath = new List<Guid>();
        private List<Guid> _pinnedEvaluationInstancePath = new List<Guid>();

        public Instance GetPinnedEvaluationInstance()
        { 
            return NodeOperations.GetInstanceFromIdPath(_pinnedEvaluationInstancePath);
        }
    }
}