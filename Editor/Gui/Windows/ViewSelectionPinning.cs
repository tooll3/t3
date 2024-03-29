using ImGuiNET;
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
    internal class ViewSelectionPinning
    {
        public void DrawPinning()
        {
            if (!TryGetPinnedOrSelectedInstance(out var pinnedOrSelectedInstance, out var canvas))
                return;
            
            var selection = canvas.NodeSelection;

            bool shouldPin = false;
            FrameStats.AddPinnedId(pinnedOrSelectedInstance.SymbolChildId);    
            
            if (CustomComponents.IconButton(Icon.Pin, 
                                                   
                                                  new Vector2(T3Style.ToolBarHeight, T3Style.ToolBarHeight) * T3Ui.UiScaleFactor,
                                            _isPinned ? CustomComponents.ButtonStates.Activated : CustomComponents.ButtonStates.Dimmed
                                                  ))
            {
                
                if (_isPinned)
                {
                    // Keep pinned if pinned operator changed
                    var oneSelected = selection.Selection.Count == 1;
                    var selectedOp= selection.GetFirstSelectedInstance();
                    var opChanged = pinnedOrSelectedInstance != selectedOp;
                    if (!opChanged || !oneSelected)
                    {
                        Unpin();
                    }
                    else
                    {
                        shouldPin = true;
                    }
                }
                else
                {
                    shouldPin = true;
                }
                
                if (shouldPin)
                    PinSelectionToView(canvas);
            }
            CustomComponents.TooltipForLastItem("Pin output to active operator.");
            
            
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            var suffix = _isPinned ? " (pinned)" : " (selected)";

            if (TryGetPinnedEvaluationInstance(canvas.Structure, out var pinnedEvaluationInstance))
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
                        Unpin();
                    }

                    var instanceSelectedInGraph = _pinnedInstanceCanvas!.NodeSelection.GetFirstSelectedInstance();
                    if (instanceSelectedInGraph != pinnedOrSelectedInstance)
                    {
                        if (ImGui.MenuItem("Pin Selection to View"))
                        {
                            PinSelectionToView(canvas);
                        }
                    }
                }
                else
                {
                    if (ImGui.MenuItem("Pin Selection to View"))
                    {
                        PinSelectionToView(canvas);
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
                        PinSelectionAsEvaluationStart(selection?.GetFirstSelectedInstance());
                    }
                }

                if (GraphWindow.Focused != null)
                {
                    if (ImGui.MenuItem("Show in Graph"))
                    {
                        var parentInstance = pinnedOrSelectedInstance.Parent;
                        var parentSymbolUi = parentInstance.GetSymbolUi();
                        var instanceChildUi = parentSymbolUi.ChildUis[pinnedOrSelectedInstance.SymbolChildId];
                        selection.SetSelectionToChildUi(instanceChildUi, pinnedOrSelectedInstance);
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

        private void PinSelectionToView(GraphCanvas canvas)
        {
            var firstSelectedInstance = canvas.NodeSelection.GetFirstSelectedInstance();
            PinInstance(firstSelectedInstance, canvas);
            //_pinnedEvaluationInstancePath = null;
        }

        private void PinSelectionAsEvaluationStart(Instance instance)
        {
            _pinnedEvaluationInstancePath = instance.InstancePath;
        }

        public bool TryGetPinnedOrSelectedInstance(out Instance instance, out GraphCanvas canvas)
        {
            var window = GraphWindow.Focused;
            canvas = window?.GraphCanvas;
            instance = null;

            if (!_isPinned)
            {
                if (window == null)
                {
                    return false;
                }

                instance = canvas.NodeSelection.GetFirstSelectedInstance();
            }
            else if (!_pinnedInstanceCanvas!.Destroyed)
            {
                instance = _pinnedInstanceCanvas.Structure.GetInstanceFromIdPath(_pinnedInstancePath);
                canvas = _pinnedInstanceCanvas;
            }
            else
            {
                Unpin();
                if (window != null)
                {
                    instance = canvas.NodeSelection.GetFirstSelectedInstance();
                }
            }

            return instance != null;
        }


        public void PinInstance(Instance instance, GraphCanvas canvas)
        {
            _pinnedInstancePath = instance.InstancePath;
            _pinnedInstanceCanvas = canvas;
            _isPinned = true;
        }
        
        private void Unpin()
        {
            _isPinned = false;
            _pinnedInstanceCanvas = null;
            _pinnedInstancePath = null;
        }

        public bool TryGetPinnedEvaluationInstance(Structure structure, out Instance? instance)
        {
            instance = structure.GetInstanceFromIdPath(_pinnedEvaluationInstancePath);
            return instance != null;
        }

        private bool _isPinned;
        private GraphCanvas? _pinnedInstanceCanvas;
        private IReadOnlyList<Guid> _pinnedInstancePath = Array.Empty<Guid>();
        private IReadOnlyList<Guid> _pinnedEvaluationInstancePath = Array.Empty<Guid>();
    }
}