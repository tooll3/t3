using System;
using System.Collections.Generic;
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Core.Utils;
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
    /// <summary>
    /// A helper class to render an image output into the background of the graph window 
    /// </summary>
    public class GraphImageBackground
    {

        public bool IsActive => _backgroundNodePath != null;
        public bool HasInteractionFocus;

        public Instance OutputInstance
        {
            set => _backgroundNodePath = OperatorUtils.BuildIdPathForInstance(value);
            get => NodeOperations.GetInstanceFromIdPath(_backgroundNodePath);
        }
        
        
        public void DrawResolutionSelector()
        {
            ResolutionHandling.DrawSelector(ref _selectedResolution, null);
        }

        public void Draw(float imageOpacity)
        {
            if (_backgroundNodePath == null)
                return;

            var selectedInstance = NodeSelection.GetSelectedInstance();
            if (selectedInstance is ICamera camera)
            {
                _cameraInteraction.Update(camera, HasInteractionFocus);
            }

            _evaluationContext.ShowGizmos = T3.Core.Operator.GizmoVisibility.Off;
            _evaluationContext.Reset();
            _imageCanvas.SetViewMode(ImageOutputCanvas.Modes.Fitted);
            _imageCanvas.PreventMouseInteraction = true;
            _imageCanvas.Update();

            var windowContentRegionMin = ImGui.GetWindowContentRegionMin() + new Vector2(0, 0);
            ImGui.SetCursorPos(windowContentRegionMin);

            var instanceForOutput = NodeOperations.GetInstanceFromIdPath(_backgroundNodePath);

            if (instanceForOutput == null || instanceForOutput.Outputs.Count == 0)
                return;

            var viewOutput = instanceForOutput.Outputs[0];
            var preventCameraInteraction = !HasInteractionFocus
                                           || !string.IsNullOrEmpty(FrameStats.Last.OpenedPopUpName);
            _camSelectionHandling.Update(instanceForOutput, instanceForOutput.Outputs[0].ValueType, preventCameraInteraction);

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

            if (imageOpacity < 1)
            {
                ImGui.GetWindowDrawList().AddRectFilled(Vector2.Zero, Vector2.One * 10000, T3Style.Colors.Background.Fade(1 - imageOpacity));
            }
        }

        public void DrawToolbarItems()
        {
            if (!IsActive)
                return;

            //ImGui.SameLine();
            if (ImGui.Button("Clear BG"))
            {
                ClearBackground();
            }

            ImGui.SameLine();
            DrawResolutionSelector();
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

            ImGui.Checkbox("ControlBackground", ref HasInteractionFocus);
        }

        public void ClearBackground()
        {
            _backgroundNodePath = null;
            HasInteractionFocus = false;
        }

        public GizmoVisibility ShowGizmos;

        private readonly ImageOutputCanvas _imageCanvas = new();
        private readonly CameraSelectionHandling _camSelectionHandling = new();
        private readonly EvaluationContext _evaluationContext = new();
        private ResolutionHandling.Resolution _selectedResolution = ResolutionHandling.DefaultResolution;
        private readonly CameraInteraction _cameraInteraction = new();
        private List<Guid> _backgroundNodePath;

    }
}