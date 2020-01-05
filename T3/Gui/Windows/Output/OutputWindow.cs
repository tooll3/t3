using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Gui.Graph.Interaction;
using T3.Gui.OutputUi;

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
            WindowFlags = ImGuiWindowFlags.NoScrollbar;

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
            ImGui.BeginChild("##content", new Vector2(0, ImGui.GetWindowHeight()), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
            {
                _imageCanvas.NoMouseInteraction = CameraSelectionHandling.SelectedCamera != null;
                _imageCanvas.Update();

                _cameraInteraction.Update(CameraSelectionHandling.SelectedCamera);

                // move down to avoid overlapping with toolbar
                ImGui.SetCursorPos(ImGui.GetWindowContentRegionMin() + new Vector2(0, 40));
                DrawOutput(_pinning.GetSelectedInstance());

                DrawToolbar();
            }
            ImGui.EndChild();
        }

        private void DrawToolbar()
        {
            ImGui.SetCursorPos(ImGui.GetWindowContentRegionMin());
            _pinning.DrawPinning();

            if (ImGui.Button("1:1"))
            {
                _imageCanvas.SetScaleToMatchPixels();
                _imageCanvas.SetViewMode(ImageOutputCanvas.Modes.Pixel);
            }

            ImGui.SameLine();

            if (ImGui.Button("M"))
            {
                _imageCanvas.SetViewMode(ImageOutputCanvas.Modes.Fitted);
            }

            ImGui.SameLine();

            CameraSelectionHandling.DrawCameraSelection(_pinning, ref _selectedCameraId);
            ResolutionHandling.DrawSelector(ref _selectedResolution, _resolutionDialog);
        }

        private void DrawOutput(Instance instance)
        {
            if (instance == null)
                return;

            if (instance.Outputs.Count <= 0)
                return;

            var symbolUi = SymbolUiRegistry.Entries[instance.Symbol.Id];

            var firstOutput = instance.Outputs[0];
            if (!symbolUi.OutputUis.ContainsKey(firstOutput.Id))
                return;

            IOutputUi outputUi = symbolUi.OutputUis[firstOutput.Id];

            if (_selectedResolution.IsAdaptive)
            {
                var size = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
                _evaluationContext.RequestedResolution.Width = (int)size.X;
                _evaluationContext.RequestedResolution.Height = (int)size.Y;
            }
            else
            {
                _evaluationContext.RequestedResolution.Width = _selectedResolution.Width;
                _evaluationContext.RequestedResolution.Height = _selectedResolution.Height;
            }
            outputUi.DrawValue(firstOutput, _evaluationContext);
        }

        private readonly EvaluationContext _evaluationContext = new EvaluationContext();
        private static readonly List<Window> OutputWindowInstances = new List<Window>();
        private readonly ImageOutputCanvas _imageCanvas = new ImageOutputCanvas();
        private readonly SelectionPinning _pinning = new SelectionPinning();
        private readonly CameraInteraction _cameraInteraction = new CameraInteraction();

        private Guid _selectedCameraId = Guid.Empty;
        private static int _instanceCounter;
        private ResolutionHandling.Resolution _selectedResolution = ResolutionHandling.Resolutions[0];

        private readonly EditResolutionDialog _resolutionDialog = new EditResolutionDialog();
    }
}