using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Operators.Types.Id_746d886c_5ab6_44b1_bb15_f3ce2fadf7e6;

namespace T3.Gui.Windows.Output
{
    public class CameraSelectionHandling
    {
        /// <summary>
        /// 
        /// </summary>
        public void DrawCameraSelection(ViewSelectionPinning pinning, ref Guid selectedCameraId)
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
                    SelectedCameraOp = null;
                    return;
                }

                var idForLambda = selectedCameraId;
                SelectedCameraOp = activeCamerasInComposition.FirstOrDefault(cam => cam.SymbolChildId == idForLambda);

                if (SelectedCameraOp == null)
                {
                    SelectedCameraOp = activeCamerasInComposition.First();
                    selectedCameraId = SelectedCameraOp.SymbolChildId;
                }
                else if (selectedCameraId == Guid.Empty)
                {
                    selectedCameraId = activeCamerasInComposition.First().SymbolChildId;
                }
            }

            ImGui.SetNextItemWidth(115);

            var selectedSymbolChild = isCameraControlDisabled
                                          ? null
                                          : SymbolRegistry.Entries[instanceSelectedInOutput.Parent.Symbol.Id]
                                                          .Children
                                                          .Single(child => child.Id == SelectedCameraOp.SymbolChildId);

            var label = isCameraControlDisabled ? "Viewer Cam" : selectedSymbolChild.ReadableName;

            if (ImGui.BeginCombo("##CameraSelection", label))
            {
                if (ImGui.Selectable("No Camera", selectedCameraId == DisableCameraId))
                {
                    SelectedCameraOp = null;
                    selectedCameraId = DisableCameraId;
                    
                }

                foreach (var cam in activeCamerasInComposition)
                {
                    ImGui.PushID(cam.SymbolChildId.GetHashCode());
                    {
                        var symbolChild = SymbolRegistry.Entries[instanceSelectedInOutput.Parent.Symbol.Id].Children
                                                        .Single(child => child.Id == cam.SymbolChildId);
                        ImGui.Selectable(symbolChild.ReadableName, cam == SelectedCameraOp);
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

        public static readonly Guid DisableCameraId = Guid.NewGuid();
        public Camera SelectedCameraOp { get; private set; }
    }
}