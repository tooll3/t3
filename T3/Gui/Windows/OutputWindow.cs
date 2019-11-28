using System;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Graph.Interaction;
using T3.Gui.OutputUi;
using T3.Gui.UiHelpers;
using T3.Operators.Types;

namespace T3.Gui.Windows
{
    public class OutputWindow : Window
    {
        public OutputWindow()
        {
            Config.Title = "Output##" + _instanceCounter;
            Config.Visible = true;

            AllowMultipleInstances = true;
            Config.Visible = true;
            WindowFlags = ImGuiWindowFlags.NoScrollbar;

            _instanceCounter++;
            _outputWindowInstances.Add(this);
        }

        private static readonly List<Window> _outputWindowInstances = new List<Window>();

        protected override void DrawAllInstances()
        {
            // Wrap inside list to enable removable of members during iteration
            foreach (var w in _outputWindowInstances.ToList())
            {
                w.DrawOneInstance();
            }
        }

        protected override void Close()
        {
            _outputWindowInstances.Remove(this);
        }

        protected override void AddAnotherInstance()
        {
            new OutputWindow();
        }

        protected override void DrawContent()
        {
            ImGui.BeginChild("##content", new Vector2(0, ImGui.GetWindowHeight()), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
            {
                _pinning.UpdateSelection();
                //var camera = FindCameraInstance();
                _imageCanvas.NoMouseInteraction = _selectedCamera != null;
                _imageCanvas.Draw();

                ImGui.SetCursorPos(ImGui.GetWindowContentRegionMin() + new Vector2(0, 40));
                //_cameraInteraction.SetCameraInstance(camera);
                _cameraInteraction.Update(_selectedCamera);
                DrawSelection(_pinning.SelectedInstance, _pinning.SelectedUi);
                DrawToolbar();

                ImGui.SetCursorPos(new Vector2(0, 0));
            }
            ImGui.EndChild();
        }

        public override List<Window> GetInstances()
        {
            return _outputWindowInstances;
        }

        private IEnumerable<Camera> FindCameras()
        {
            return _pinning.SelectedInstance.Parent?.Children.OfType<Camera>();
        }

        
        // private Camera FindCameraInstance()
        // {
        //     if (_pinning.SelectedInstance.Parent == null)
        //         return null;
        //
        //     var obj = _pinning.SelectedInstance.Parent.Children.FirstOrDefault(child => child.Type == typeof(Camera));
        //     var cam = obj as Camera;
        //     return cam;
        // }

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

            DrawCameraSelection();
        }

        private void DrawCameraSelection()
        {
            var cameras = FindCameras().ToArray();
            if (cameras.Length <= 0)
                return;

            _selectedCamera = cameras.FirstOrDefault(cam => cam.Id == _selectedCameraId);
            if (_selectedCamera == null)
            {
                _selectedCamera = cameras.First();
                _selectedCameraId = _selectedCamera.Id;
            }
            else if (_selectedCameraId == Guid.Empty)
            {
                _selectedCameraId = cameras.First().Id;
            }

            ImGui.SetNextItemWidth(100);
            if (ImGui.BeginCombo("##CameraSelection", _selectedCamera.Symbol.Name))
            {
                foreach (var cam in FindCameras())
                {
                    ImGui.PushID(cam.Id.GetHashCode());
                    {
                        var symbolChild = SymbolRegistry.Entries[_pinning.SelectedInstance.Parent.Symbol.Id].Children.Single(child => child.Id == cam.Id);
                        ImGui.Selectable(symbolChild.ReadableName, cam == _selectedCamera);
                        if (ImGui.IsItemActivated())
                        {
                            _selectedCameraId = cam.Id;
                        }

                        if (ImGui.IsItemHovered())
                        {
                            T3Ui.AddHoveredId(cam.Id);
                        }
                    }
                    ImGui.PopID();
                }
            }
        }

        private Guid _selectedCameraId = Guid.Empty;

        private static void DrawSelection(Instance selectedInstance, SymbolUi selectedUi)
        {
            if (selectedInstance == null)
                return;

            if (selectedInstance.Outputs.Count <= 0)
                return;

            var firstOutput = selectedInstance.Outputs[0];
            if (!selectedUi.OutputUis.ContainsKey(firstOutput.Id))
                return;

            IOutputUi outputUi = selectedUi.OutputUis[firstOutput.Id];
            outputUi.DrawValue(firstOutput);
        }

        private readonly ImageOutputCanvas _imageCanvas = new ImageOutputCanvas();
        private readonly SelectionPinning _pinning = new SelectionPinning();
        private readonly CameraInteraction _cameraInteraction = new CameraInteraction();

        //private static readonly List<OutputWindow> WindowInstances = new List<OutputWindow>();
        static int _instanceCounter;
        private Camera _selectedCamera;

    }
}