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
            var selectedInstance = GetSelectedInstance();
            if (selectedInstance == null)
                return;

            if (CustomComponents.ToggleButton(Icon.Pin, "##pin", ref _isPinned, new Vector2(T3Style.ToolBarHeight, T3Style.ToolBarHeight)))
            {
                if (_isPinned)
                    SetPinningToSelection();
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(250);
            var suffix = _isPinned ? " (pinned)" : " (selected)";

            if (ImGui.BeginCombo("##pinning", selectedInstance.Symbol.Name + suffix))
            {
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(6, 6));
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 6));
                if (_isPinned)
                {
                    if (ImGui.MenuItem("Unpin view"))
                    {
                        _isPinned = false;
                    }
                }
                else
                {
                    if (ImGui.MenuItem("Pin View To Selection"))
                    {
                        _isPinned = true;
                        SetPinningToSelection();
                    }
                }

                if (GraphCanvas.Current.CompositionOp != null)
                {
                    if (ImGui.MenuItem("Show in Graph"))
                    {
                        var parentInstance = selectedInstance.Parent;
                        var parentSymbolUi = SymbolUiRegistry.Entries[parentInstance.Symbol.Id];
                        var instanceChildUi = parentSymbolUi.ChildUis.Single(childUi => childUi.Id == selectedInstance.SymbolChildId);
                        SelectionManager.SetSelection(instanceChildUi, selectedInstance);
                        SelectionManager.FitViewToSelection();
                    }
                }

                if (selectedInstance.Outputs.Count > 1)
                {
                    if (ImGui.BeginMenu("Show Output..."))
                    {
                        foreach (var output in selectedInstance.Outputs)
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

        public void SetPinningToSelection()
        {
            _pinnedInstancePath = NodeOperations.BuildIdPathForInstance(SelectionManager.GetSelectedInstance());
        }

        public Instance GetSelectedInstance()
        {
            if (!_isPinned)
                return SelectionManager.GetSelectedInstance();

            var instance = NodeOperations.GetInstanceFromIdPath(_pinnedInstancePath);
            return instance ?? SelectionManager.GetSelectedInstance();
        }

        private bool _isPinned;
        private List<Guid> _pinnedInstancePath = new List<Guid>();
    }
}