using System;
using System.Collections.Generic;
using ImGuiNET;
using SharpDX;
using T3.Core.Operator;
using T3.Gui.Graph.Interaction;
using T3.Gui.OutputUi;
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

        public Instance ShownInstance => _pinning.GetSelectedInstance();

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
            if (instance == null || instance.Outputs.Count <= 0)
                return;

            var symbolUi = SymbolUiRegistry.Entries[instance.Symbol.Id];

            var firstOutput = instance.Outputs[0];
            if (!symbolUi.OutputUis.TryGetValue(firstOutput.Id, out IOutputUi outputUi))
                return;

            _evaluationContext.Reset();

            if (_selectedResolution.UseAsAspectRatio)
            {
                var windowSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
                var windowAspectRatio = windowSize.X / windowSize.Y;
                var requestedAspectRatio = (float)_selectedResolution.Size.Width / _selectedResolution.Size.Height;

                var size = (requestedAspectRatio > windowAspectRatio)
                               ? new Size2((int)windowSize.X, (int)(windowSize.X / requestedAspectRatio))
                               : new Size2((int)(windowSize.Y * requestedAspectRatio), (int)windowSize.Y);

                _evaluationContext.RequestedResolution = size;
            }
            else
            {
                _evaluationContext.RequestedResolution = _selectedResolution.Size;
            }

            outputUi.DrawValue(firstOutput, _evaluationContext);
        }

        private readonly EvaluationContext _evaluationContext = new EvaluationContext();
        public static readonly List<Window> OutputWindowInstances = new List<Window>();
        private readonly ImageOutputCanvas _imageCanvas = new ImageOutputCanvas();
        private readonly SelectionPinning _pinning = new SelectionPinning();
        private readonly CameraInteraction _cameraInteraction = new CameraInteraction();

        private Guid _selectedCameraId = Guid.Empty;
        private static int _instanceCounter;
        private ResolutionHandling.Resolution _selectedResolution = ResolutionHandling.DefaultResolution;

        private readonly EditResolutionDialog _resolutionDialog = new EditResolutionDialog();
    }
}