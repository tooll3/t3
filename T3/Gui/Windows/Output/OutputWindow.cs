using System;
using System.Collections.Generic;
using ImGuiNET;
using SharpDX;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Gui.Graph.Interaction;
using T3.Gui.Interaction;
using T3.Gui.Interaction.TransformGizmos;
using T3.Gui.OutputUi;
using T3.Gui.Selection;
using T3.Gui.Styling;
using Vector2 = System.Numerics.Vector2;

namespace T3.Gui.Windows.Output
{
    public class OutputWindow : Window
    {
        #region Window implementation
        public OutputWindow()
        {
            Config.Title = "Output##" + _instanceCounter;
            Config.Visible = true;

            AllowMultipleInstances = true;
            Config.Visible = true;
            WindowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

            _instanceCounter++;
            OutputWindowInstances.Add(this);
        }

        protected override void DrawAllInstances()
        {
            // Convert to array to enable removable of members during iteration
            foreach (var w in OutputWindowInstances.ToArray())
            {
                w.DrawOneInstance();
            }
        }

        protected override void Close()
        {
            OutputWindowInstances.Remove(this);
        }

        protected override void AddAnotherInstance()
        {
            // ReSharper disable once ObjectCreationAsStatement
            new OutputWindow();
        }

        public override List<Window> GetInstances()
        {
            return OutputWindowInstances;
        }
        #endregion

        protected override void DrawContent()
        {
            
            ImGui.BeginChild("##content", new Vector2(0, ImGui.GetWindowHeight()), false,
                             ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollWithMouse);
            {
                // Draw output
                _imageCanvas.SetAsCurrent();

                // Move down to avoid overlapping with toolbar
                ImGui.SetCursorPos(ImGui.GetWindowContentRegionMin() + new Vector2(0, 40));
                var drawnInstance = Pinning.GetPinnedOrSelectedInstance();
                var drawnType = DrawOutput(drawnInstance, Pinning.GetPinnedEvaluationInstance());
                _imageCanvas.Deactivate();

                _camSelectionHandling.Update(drawnInstance, drawnType);
                _imageCanvas.PreventMouseInteraction = _camSelectionHandling.PreventCameraInteraction;
                _imageCanvas.Update();
                DrawToolbar();
            }
            ImGui.EndChild();
        }


        private void DrawToolbar()
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Color(0.6f).Rgba);
            ImGui.SetCursorPos(ImGui.GetWindowContentRegionMin());
            Pinning.DrawPinning();

            ImGui.PushStyleColor(ImGuiCol.Text, Math.Abs(_imageCanvas.Scale.X - 1f) < 0.001f ? Color.Black.Rgba : Color.White);
            if (ImGui.Button("1:1"))
            {
                _imageCanvas.SetScaleToMatchPixels();
                _imageCanvas.SetViewMode(ImageOutputCanvas.Modes.Pixel);
            }

            ImGui.PopStyleColor();

            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Text, _imageCanvas.ViewMode == ImageOutputCanvas.Modes.Fitted ? Color.Black.Rgba : Color.White);
            if (ImGui.Button("Fit") || KeyboardBinding.Triggered(UserActions.FocusSelection))
            {
                _imageCanvas.SetViewMode(ImageOutputCanvas.Modes.Fitted);
            }

            ImGui.PopStyleColor();

            ImGui.SameLine();

            var showGizmos = _evaluationContext.ShowGizmos != T3.Core.Operator.GizmoVisibility.Off;
            if (CustomComponents.ToggleIconButton(Icon.Grid, "##gizmos", ref showGizmos, Vector2.One * ImGui.GetFrameHeight()))
            {
                _evaluationContext.ShowGizmos = showGizmos
                                                    ? T3.Core.Operator.GizmoVisibility.On
                                                    : T3.Core.Operator.GizmoVisibility.Off;
            }

            ImGui.SameLine();

            _camSelectionHandling.DrawCameraControlSelection(Pinning);
            ResolutionHandling.DrawSelector(ref _selectedResolution, _resolutionDialog);
            
            ImGui.SameLine();
            ColorEditButton.Draw(ref _backgroundColor, new Vector2(ImGui.GetFrameHeight(), ImGui.GetFrameHeight()));
            ImGui.PopStyleColor();
        }

        
        private Type DrawOutput(Instance instanceForOutput, Instance instanceForEvaluation = null)
        {
            if (instanceForEvaluation == null)
                instanceForEvaluation = instanceForOutput;

            if (instanceForEvaluation == null || instanceForEvaluation.Outputs.Count <= 0)
                return null;

            var evaluatedSymbolUi = SymbolUiRegistry.Entries[instanceForEvaluation.Symbol.Id];

            // Todo: support different outputs...
            var evalOutput = instanceForEvaluation.Outputs[0];
            if (!evaluatedSymbolUi.OutputUis.TryGetValue(evalOutput.Id, out IOutputUi evaluatedOutputUi))
                return null;

            if (_imageCanvas.ViewMode !=  ImageOutputCanvas.Modes.Fitted 
                && evaluatedOutputUi is CommandOutputUi)
            {
                _imageCanvas.SetViewMode(ImageOutputCanvas.Modes.Fitted);
            }
            
            // Prepare context
            _evaluationContext.Reset();
            _evaluationContext.BypassCameras = _camSelectionHandling.BypassCamera;
            _evaluationContext.RequestedResolution = _selectedResolution.ComputeResolution();
            
            // Set camera
            //var usedCam = _lastInteractiveCam ?? _outputWindowViewCamera;
            if (_camSelectionHandling.CameraForRendering != null)
            {
                _evaluationContext.SetViewFromCamera(_camSelectionHandling.CameraForRendering);
            }
            else
            {
                Log.Error("Viewer camera not undefined");
            }
            _evaluationContext.BackgroundColor = _backgroundColor;

            // Ugly hack to hide final target
            if (instanceForOutput != instanceForEvaluation)
            {
                ImGui.BeginChild("hidden", Vector2.One * 1);
                {
                    evaluatedOutputUi.DrawValue(evalOutput, _evaluationContext);
                }
                ImGui.EndChild();

                if (instanceForOutput == null || instanceForOutput.Outputs.Count == 0)
                    return null;

                var viewOutput = instanceForOutput.Outputs[0];
                var viewSymbolUi = SymbolUiRegistry.Entries[instanceForOutput.Symbol.Id];
                if (!viewSymbolUi.OutputUis.TryGetValue(viewOutput.Id, out IOutputUi viewOutputUi))
                    return null;

                viewOutputUi.DrawValue(viewOutput, _evaluationContext, recompute: false);
                return viewOutputUi.Type;
            }
            else
            {
                evaluatedOutputUi.DrawValue(evalOutput, _evaluationContext);
                return evalOutput.ValueType;
            }
        }
        
        public Instance ShownInstance => Pinning.GetPinnedOrSelectedInstance();
        public static readonly List<Window> OutputWindowInstances = new();
        public ViewSelectionPinning Pinning { get; } = new();
        
        private System.Numerics.Vector4 _backgroundColor = new(0.1f, 0.1f, 0.1f, 1.0f);
        private readonly EvaluationContext _evaluationContext = new();
        private readonly ImageOutputCanvas _imageCanvas = new();
        private readonly CameraSelectionHandling _camSelectionHandling = new();
        private static int _instanceCounter;
        private ResolutionHandling.Resolution _selectedResolution = ResolutionHandling.DefaultResolution;
        private readonly EditResolutionDialog _resolutionDialog = new();
    }
}