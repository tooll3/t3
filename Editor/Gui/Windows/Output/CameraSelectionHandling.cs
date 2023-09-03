using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Interaction.Camera;
using T3.Editor.Gui.Interaction.TransformGizmos;
using T3.Editor.Gui.Styling;
using T3.Operators.Types.Id_746d886c_5ab6_44b1_bb15_f3ce2fadf7e6;

namespace T3.Editor.Gui.Windows.Output
{
    public class CameraSelectionHandling
    {
        public ICamera CameraForRendering { get; private set; }
        public bool BypassCamera { get; private set; }
        
        public bool PreventCameraInteraction { get; private set; }
        
        public bool PreventImageCanvasInteraction { get; private set; }
        
        private enum ControlModes
        {
            /// <summary>
            /// If selected op is Camera, manipulate this.
            /// else
            ///   if playback is running
            ///      move ViewerCamera to first camera in graph  
            ///   else
            ///      manipulate ViewerCamera
            /// </summary>
            SceneViewerFollowing,

            /// <summary>
            /// Always use 3rd person view of ViewerCamera, regardless of selected Operator.
            /// </summary>
            UseViewer,

            /// <summary>
            /// If rendered op is Command-Type and manipulate first found camera in graph
            /// or if rendered Op is ImageType and selected Op is Camera manipulate camera 
            /// </summary>
            AutoUseFirstCam,
            
            /// <summary>
            /// Uses the camera picked from dropdown for manipulation.
            /// If camera wasn't recently updated, fall back to last other mode. 
            /// </summary>
            PickedACamera,
        }
        
        public void Update(Instance drawnInstance, Type drawnType, bool preventInteractions = false)
        {
            _hasAnimationTimeChanged = Math.Abs(_lastUpdateTime - Playback.Current.TimeInBars) > 0.001f;
            _lastUpdateTime = Playback.Current.TimeInBars;
            PreventImageCanvasInteraction = false;
            
            _drawnInstance = drawnInstance;
            if (drawnInstance == null)
            {
                PreventCameraInteraction = true;
                CameraForRendering = _outputWindowViewCamera;
                _recentlyUsedCameras = new List<ICamera>();
                return;
            }

            // Update recently used cameras (this is expensive!)
            UpdateRecentCameras(drawnInstance);

            ICamera cameraForManipulation = null;
            CameraForRendering = null;

            PreventCameraInteraction = preventInteractions;


            if (_controlMode == ControlModes.PickedACamera)
            {
                var isPickStillValid = false;
                foreach (var recentCam in _recentlyUsedCameras)
                {
                    if (recentCam is not Instance camInstance)
                        continue;
                    
                    if (camInstance.SymbolChildId == _pickedCameraId)
                    {
                        cameraForManipulation = recentCam;
                        CameraForRendering = _outputWindowViewCamera;
                        isPickStillValid = true;
                        break;
                    }
                }

                if (!isPickStillValid)
                {
                    _controlMode = _lastControlMode;
                }
            }
            
            switch (_controlMode)
            {
                case ControlModes.SceneViewerFollowing:
                {
                    if (drawnType != typeof(Command))
                    {
                        PreventCameraInteraction = true;
                    }
                    cameraForManipulation = _outputWindowViewCamera;
                    
                    if (_firstCamInGraph != null)
                    {
                        PreventImageCanvasInteraction = true;
                        var isCamOpSelected = NodeSelection.GetFirstSelectedInstance() == _firstCamInGraph;
                        if (isCamOpSelected)
                        {
                            cameraForManipulation = _firstCamInGraph;
                        }
                        
                        if (_hasAnimationTimeChanged)
                        {
                            _outputWindowViewCamera.CameraPosition = _firstCamInGraph.CameraPosition;
                            _outputWindowViewCamera.CameraTarget = _firstCamInGraph.CameraTarget;
                            _outputWindowViewCamera.CameraRoll = _firstCamInGraph.CameraRoll;
                        }
                    }
                    
                    BypassCamera = true;
                    break;
                }
                
                case ControlModes.UseViewer:
                    cameraForManipulation = _outputWindowViewCamera;
                    BypassCamera = true;
                    break;

                case ControlModes.AutoUseFirstCam:
                {
                    if (_firstCamInGraph != null)
                    {
                        var isCamOpSelected = NodeSelection.GetFirstSelectedInstance() == _firstCamInGraph;
                        if (!isCamOpSelected)
                        {
                            cameraForManipulation = _outputWindowViewCamera;
                        }
                        else
                        {
                            //PreventCameraInteraction = true;
                            PreventImageCanvasInteraction = true;
                            cameraForManipulation = _firstCamInGraph;    
                        }
                        CameraForRendering = _outputWindowViewCamera;
                        _cameraInteraction.ResetCamera(_outputWindowViewCamera);
                    }
                    else
                    {
                        cameraForManipulation = _outputWindowViewCamera;
                    }
                    BypassCamera = false;
                    break;
                }
            }

            if (_controlMode != ControlModes.PickedACamera)
            {
                CameraForRendering = cameraForManipulation;
            }

            if (CameraForRendering == null)
            {
                CameraForRendering = _outputWindowViewCamera;
            }

            if (TransformGizmoHandling.IsDragging)
                PreventCameraInteraction = true;
            
            if (!PreventCameraInteraction && cameraForManipulation != null)
            {
                _cameraInteraction.Update(cameraForManipulation, !PreventCameraInteraction);
            }
        }

        private void UpdateRecentCameras(Instance drawnInstance)
        {
            _recentlyUsedCameras.Clear();
            _firstCamInGraph = null;
            
            var parentInstance = drawnInstance.Parent;
            if (parentInstance != null)
            {
                var children = parentInstance.Children;
                foreach (var child in children)
                {
                    if (child is not ICamera cam2)
                        continue;

                    if (child.Outputs[0].DirtyFlag.FramesSinceLastUpdate > 1)
                        continue;

                    if (_firstCamInGraph == null)
                        _firstCamInGraph = cam2;

                    _recentlyUsedCameras.Add(cam2);
                }
            }

            var selectedInstance = NodeSelection.GetSelectedInstance();
            if (selectedInstance is Camera selectedCamera
                && !_recentlyUsedCameras.Contains(selectedCamera))
            {
                if (_recentlyUsedCameras.Count == 0)
                {
                    _firstCamInGraph = selectedCamera;
                }
                _recentlyUsedCameras.Add(selectedCamera);
            }
        }

        const string SceneViewerFollowingLabel = "Viewer (Following)";
        const string SceneViewerModeLabel = "Viewer";
        const string CameraModeLabel = "Camera";

        public void DrawCameraControlSelection()
        {
            ImGui.SetNextItemWidth(115);

            var label = String.Empty;
            
            switch (_controlMode)
            {
                case ControlModes.SceneViewerFollowing:
                    label = SceneViewerFollowingLabel;
                    break;
                
                case ControlModes.UseViewer:
                    label = SceneViewerModeLabel;
                    break;
                
                case ControlModes.AutoUseFirstCam:
                    label = CameraModeLabel;
                    break;
                
                case ControlModes.PickedACamera:
                    label = "Locked to Cam";
                    break;
            }

            if (ImGui.BeginCombo("##CameraSelection", label))
            {
                if (ImGui.Selectable(CameraModeLabel, _controlMode == ControlModes.AutoUseFirstCam))
                {
                    _controlMode = ControlModes.AutoUseFirstCam;
                    _pickedCameraId = Guid.Empty;
                }
                CustomComponents.TooltipForLastItem("Shows the first scene camera. For manipulation select the camera operator in graph or in the list below.");
                if (ImGui.IsItemHovered() && _firstCamInGraph is Instance camInstance)
                    FrameStats.AddHoveredId(camInstance.SymbolChildId);

                if (ImGui.Selectable(SceneViewerModeLabel, _controlMode == ControlModes.UseViewer))
                {
                    _controlMode = ControlModes.UseViewer;
                    _pickedCameraId = Guid.Empty;
                }
                CustomComponents.TooltipForLastItem("Ignores scene cameras. This can be useful in combination with [ShowCamGizmos].");
                
                if (ImGui.Selectable(SceneViewerFollowingLabel, _controlMode == ControlModes.SceneViewerFollowing))
                {
                    _controlMode = ControlModes.SceneViewerFollowing;
                    _pickedCameraId = Guid.Empty;
                }
                if (ImGui.IsItemHovered() && _firstCamInGraph is Instance camInstance2)
                    FrameStats.AddHoveredId(camInstance2.SymbolChildId);
                
                CustomComponents.TooltipForLastItem("During playback the scene viewer is following the scene camera. Otherwise it can be independently manipulated without affecting the scene camera.");
                
                ImGui.Separator();
                
                foreach (var cam in _recentlyUsedCameras)
                {
                    if (cam is not Instance cameraInstance)
                        continue;
                    
                    ImGui.PushID(cameraInstance.SymbolChildId.GetHashCode());
                    {
                        // This is expensive, but happens only if dropdown is open...
                        var symbolChild = SymbolRegistry.Entries[_drawnInstance.Parent.Symbol.Id].Children
                                                        .SingleOrDefault(child => child.Id == cameraInstance.SymbolChildId);

                        if (symbolChild == null)
                            continue;
                        
                        if(ImGui.Selectable("Operator: " + symbolChild.ReadableName, cameraInstance.SymbolChildId == _pickedCameraId))
                        {
                            _lastControlMode = _controlMode;
                            _controlMode = ControlModes.PickedACamera;
                            _pickedCameraId = cameraInstance.SymbolChildId;
                        }

                        if (ImGui.IsItemHovered())
                        {
                            FrameStats.AddHoveredId(cameraInstance.SymbolChildId);
                        }
                    }
                    ImGui.PopID();
                }

                ImGui.EndCombo();
            }
            else
            {
                CustomComponents.TooltipForLastItem("Camera control mode", "This affects which camera will be manipulated by the view controls. Please also review to the tooltips of the dropdown options.");

            }

            ImGui.SameLine();
        }

        public void ResetView()
        {
            _cameraInteraction.ResetView();
        }


        private ControlModes _controlMode = ControlModes.AutoUseFirstCam;
        private readonly ViewCamera _outputWindowViewCamera = new();
        private readonly CameraInteraction _cameraInteraction = new();

        private double _lastUpdateTime;
        private bool _hasAnimationTimeChanged;

        private Guid _pickedCameraId = Guid.Empty;
        
        private Instance _drawnInstance;
        private List<ICamera> _recentlyUsedCameras = new();

        private ControlModes _lastControlMode;

        private ICamera _firstCamInGraph;
    }
}