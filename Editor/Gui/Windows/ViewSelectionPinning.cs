using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.Windows.Output;
using T3.Editor.UiModel;
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
            
            // Keep pinned if pinned operator changed
            var oneSelected = NodeSelection.Selection.Count == 1;
            var selectedOp = NodeSelection.GetFirstSelectedInstance();
            var isPinnedToSelected = pinnedOrSelectedInstance == selectedOp;
            
            if (CustomComponents.IconButton(Icon.Pin,
                                            new Vector2(T3Style.ToolBarHeight, T3Style.ToolBarHeight) * T3Ui.UiScaleFactor,
                                            _isPinned ? CustomComponents.ButtonStates.Activated : CustomComponents.ButtonStates.Dimmed
                                           ))
            {
                if (_isPinned)
                {
                    _isPinned = false;
                }
                else if (selectedOp != null)
                {
                    PinInstance(selectedOp);
                }
            }
            
            CustomComponents.TooltipForLastItem("Pin output to active operator.");
            
            if (_isPinned)
            {
                ImGui.SameLine();
                if (CustomComponents.IconButton(Icon.PlayOutput,
                                                new Vector2(T3Style.ToolBarHeight, T3Style.ToolBarHeight) * T3Ui.UiScaleFactor,
                                                isPinnedToSelected ? CustomComponents.ButtonStates.Disabled : CustomComponents.ButtonStates.Normal
                                               )
                    && !isPinnedToSelected 
                    && oneSelected)
                {
                    
                        PinSelectionToView();
                }
                
                CustomComponents.TooltipForLastItem(selectedOp != null
                                                        ? $"Pin output to selected {selectedOp.Symbol.Name}."
                                                        : $"Select an operator and click to update pinning.");
            }
            
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            var suffix = _isPinned ? " (pinned)" : " (selected)";
            
            var pinnedEvaluationInstance = GetPinnedEvaluationInstance();
            if (pinnedEvaluationInstance != null)
            {
                suffix += " -> " + pinnedEvaluationInstance.Symbol.Name + " (Final)";
            }
            
            ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
            
            if (ImGui.BeginCombo("##pinning", pinnedOrSelectedInstance.Symbol.Name + suffix))
            {
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
                    if (ImGui.MenuItem("Pin Selection to View"))
                    {
                        _isPinned = true;
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
                ImGui.PopStyleVar();
                ImGui.EndCombo();
            }
            
            ImGui.PopStyleColor();
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
            
            var instance = Structure.GetInstanceFromIdPath(_pinnedInstancePath);
            if (instance != null)
                return instance;
            
            _isPinned = false;
            return NodeSelection.GetFirstSelectedInstance();
        }
        
        public Instance GetPinnedEvaluationInstance()
        {
            return Structure.GetInstanceFromIdPath(_pinnedEvaluationInstancePath);
        }
        
        private bool _isPinned;
        private List<Guid> _pinnedInstancePath = new();
        private List<Guid> _pinnedEvaluationInstancePath = new();
    }
}