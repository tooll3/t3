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
            var instanceSelectedInOutput = pinning.GetSelectedInstance();

            // ReSharper disable once UseNullPropagation
            if (instanceSelectedInOutput == null)
                return;
            
            var activeCamerasInComposition = instanceSelectedInOutput.Parent?.Children
                                                               .OfType<Camera>()
                                                               .Where(cam => cam.Outputs[0].DirtyFlag.FramesSinceLastUpdate < 1)
                                                               .ToList();

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

            ImGui.SetNextItemWidth(100);
            
            var selectedSymbolChild = SymbolRegistry.Entries[instanceSelectedInOutput.Parent.Symbol.Id].Children.Single(child => child.Id == SelectedCamera.SymbolChildId);

            if (ImGui.BeginCombo("##CameraSelection", selectedSymbolChild.ReadableName))
            {
                ImGui.Selectable("No Camera", SelectedCamera == null);
                if (ImGui.IsItemActivated())
                {
                    SelectedCamera = null;
                    selectedCameraId = Guid.Empty;
                }
                
                foreach (var cam in activeCamerasInComposition)
                {
                    ImGui.PushID(cam.SymbolChildId.GetHashCode());
                    {
                        var symbolChild = SymbolRegistry.Entries[instanceSelectedInOutput.Parent.Symbol.Id].Children.Single(child => child.Id == cam.SymbolChildId);
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
            }
            ImGui.SameLine();
        }

        public static Camera SelectedCamera { get; private set; }
    }
}