using System;
using System.Collections.Generic;
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Interaction.Camera;
using T3.Editor.Gui.OutputUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.Gui.Windows.Output;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.Graph
{
    public partial class GraphWindow
    {
        /// <summary>
        /// A helper class to render an image output into the background of the graph window 
        /// </summary>
        private class ImageBackground
        {
            internal List<Guid> BackgroundNodePath;

            internal bool IsActive => BackgroundNodePath != null;

            public void DrawResolutionSelector()
            {
                ResolutionHandling.DrawSelector(ref _selectedResolution, null);
            }
            
            public void Draw()
            {
                if (BackgroundNodePath == null)
                    return;

                var selectedInstance = NodeSelection.GetSelectedInstance();
                if (selectedInstance is ICamera camera)
                {
                    _cameraInteraction.Update(camera, UserSettings.Config.ControlGraphBackground);
                }
                
                _evaluationContext.ShowGizmos = T3.Core.Operator.GizmoVisibility.Off;
                _evaluationContext.Reset();
                _imageCanvas.SetViewMode(ImageOutputCanvas.Modes.Fitted);
                _imageCanvas.PreventMouseInteraction = true;
                _imageCanvas.Update();

                var windowContentRegionMin = ImGui.GetWindowContentRegionMin() + new Vector2(0, 0);
                ImGui.SetCursorPos(windowContentRegionMin);
                
                var instanceForOutput = NodeOperations.GetInstanceFromIdPath(BackgroundNodePath);

                if (instanceForOutput == null || instanceForOutput.Outputs.Count == 0)
                    return;

                var viewOutput = instanceForOutput.Outputs[0];
                var preventCameraInteraction = !UserSettings.Config.ControlGraphBackground;
                _camSelectionHandling.Update(instanceForOutput, instanceForOutput.Outputs[0].ValueType,preventCameraInteraction);
                
                var viewSymbolUi = SymbolUiRegistry.Entries[instanceForOutput.Symbol.Id];
                if (!viewSymbolUi.OutputUis.TryGetValue(viewOutput.Id, out IOutputUi viewOutputUi))
                    return;
                
                _imageCanvas.SetAsCurrent();
                _evaluationContext.ShowGizmos = ShowGizmos;                
                _evaluationContext.RequestedResolution = _selectedResolution.ComputeResolution();
                _evaluationContext.SetDefaultCamera();
                if (_camSelectionHandling.CameraForRendering != null)
                {
                    _evaluationContext.SetViewFromCamera(_camSelectionHandling.CameraForRendering);
                }

                viewOutputUi.DrawValue(viewOutput, _evaluationContext, recompute: true);
                _imageCanvas.Deactivate();
            }

            
            public void DrawToolbarItems()
            {
                if (!IsActive)
                    return;
                
                DrawResolutionSelector();
                ImGui.SameLine();
                if (ImGui.Button("Clear BG"))
                {
                    ClearBackground();
                }

                ImGui.SameLine();
                
                var showGizmos = ShowGizmos != T3.Core.Operator.GizmoVisibility.Off;
                if (CustomComponents.ToggleIconButton(Icon.Grid, "##gizmos", ref showGizmos, Vector2.One * ImGui.GetFrameHeight()))
                {
                    ShowGizmos = showGizmos
                                     ? T3.Core.Operator.GizmoVisibility.On
                                     : T3.Core.Operator.GizmoVisibility.Off;
                }                    
                ImGui.SameLine();
                
                _camSelectionHandling.DrawCameraControlSelection();
                ImGui.SameLine();

                ImGui.Checkbox("ControlBackground", ref UserSettings.Config.ControlGraphBackground);
            }
            
            
            public GizmoVisibility ShowGizmos;
            
            private readonly ImageOutputCanvas _imageCanvas = new();
            private readonly CameraSelectionHandling _camSelectionHandling = new();
            private readonly EvaluationContext _evaluationContext = new();
            private ResolutionHandling.Resolution _selectedResolution = ResolutionHandling.DefaultResolution;
            private readonly CameraInteraction _cameraInteraction = new();


        }
    }
}