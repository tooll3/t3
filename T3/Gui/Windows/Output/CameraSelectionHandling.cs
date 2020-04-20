using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using T3.Core.Operator;
using T3.Operators.Types.Id_746d886c_5ab6_44b1_bb15_f3ce2fadf7e6;

namespace T3.Gui.Windows.Output
{
    public static class CameraSelectionHandling
    {
        /// <summary>
        /// 
        /// </summary>
        public static void DrawCameraSelection(ViewSelectionPinning pinning, ref Guid selectedCameraId)
        {
            var instanceSelectedInOutput = pinning.GetPinnedOrSelectedInstance();
            var isCameraControlDisabled = selectedCameraId == DisableCameraId;

            // ReSharper disable once UseNullPropagation
            if (instanceSelectedInOutput == null)
                return;

            var activeCamerasInComposition = instanceSelectedInOutput.Parent?.Children
                                                                     .OfType<Camera>()
                                                                     .Where(cam => cam.Outputs[0].DirtyFlag.FramesSinceLastUpdate < 1)
                                                                     .ToList();

            if (!isCameraControlDisabled)
            {
                if (activeCamerasInComposition == null || activeCamerasInComposition.Count == 0)
                {
                    SelectedCamera = null;
                    return;
                }

                var idForLambda = selectedCameraId;
                SelectedCamera = activeCamerasInComposition.FirstOrDefault(cam => cam.SymbolChildId == idForLambda);

                if (SelectedCamera == null)
                {
                    SelectedCamera = activeCamerasInComposition.First();
                    selectedCameraId = SelectedCamera.SymbolChildId;
                }
                else if (selectedCameraId == Guid.Empty)
                {
                    selectedCameraId = activeCamerasInComposition.First().SymbolChildId;
                }
            }

            ImGui.SetNextItemWidth(100);

            var selectedSymbolChild = isCameraControlDisabled
                                          ? null
                                          : SymbolRegistry.Entries[instanceSelectedInOutput.Parent.Symbol.Id]
                                                          .Children
                                                          .Single(child => child.Id == SelectedCamera.SymbolChildId);

            var label = isCameraControlDisabled ? "No Camera" : selectedSymbolChild.ReadableName;

            if (ImGui.BeginCombo("##CameraSelection", label))
            {
                if (ImGui.Selectable("No Camera", selectedCameraId == DisableCameraId))
                {
                    SelectedCamera = null;
                    selectedCameraId = DisableCameraId;
                    
                }

                foreach (var cam in activeCamerasInComposition)
                {
                    ImGui.PushID(cam.SymbolChildId.GetHashCode());
                    {
                        var symbolChild = SymbolRegistry.Entries[instanceSelectedInOutput.Parent.Symbol.Id].Children
                                                        .Single(child => child.Id == cam.SymbolChildId);
                        ImGui.Selectable(symbolChild.ReadableName, cam == SelectedCamera);
                        if (ImGui.IsItemActivated())
                        {
                            selectedCameraId = cam.SymbolChildId;
                        }

                        if (ImGui.IsItemHovered())
                        {
                            T3Ui.AddHoveredId(cam.SymbolChildId);
                        }
                    }
                    ImGui.PopID();
                }
                ImGui.EndCombo();
            }

            ImGui.SameLine();
        }

        public static Camera SelectedCamera { get; private set; }
        public static readonly Guid DisableCameraId = Guid.NewGuid();
    }
}