using System;
using System.Linq;
using ImGuiNET;
using T3.Core.Operator;
using T3.Operators.Types.Id_746d886c_5ab6_44b1_bb15_f3ce2fadf7e6;

namespace T3.Gui.Windows.Output
{
    public static class CameraSelectionHandling
    {
        public static void DrawCameraSelection(SelectionPinning pinning, ref Guid selectedCameraId)
        {
            var instance = pinning.GetSelectedInstance();

            // ReSharper disable once UseNullPropagation
            if (instance == null)
                return;
            
            var camerasInComposition = instance.Parent?.Children.OfType<Camera>().ToList();

            if (camerasInComposition == null || camerasInComposition.Count == 0)
                return;

            var idForLambda = selectedCameraId;
            SelectedCamera = camerasInComposition.FirstOrDefault(cam => cam.SymbolChildId == idForLambda);

            if (SelectedCamera == null)
            {
                SelectedCamera = camerasInComposition.First();
                selectedCameraId = SelectedCamera.SymbolChildId;
            }
            else if (selectedCameraId == Guid.Empty)
            {
                selectedCameraId = camerasInComposition.First().SymbolChildId;
            }

            ImGui.SetNextItemWidth(100);
            
            var selectedSymbolChild = SymbolRegistry.Entries[instance.Parent.Symbol.Id].Children.Single(child => child.Id == SelectedCamera.SymbolChildId);

            if (ImGui.BeginCombo("##CameraSelection", selectedSymbolChild.ReadableName))
            {
                foreach (var cam in camerasInComposition)
                {
                    ImGui.PushID(cam.SymbolChildId.GetHashCode());
                    {
                        var symbolChild = SymbolRegistry.Entries[instance.Parent.Symbol.Id].Children.Single(child => child.Id == cam.SymbolChildId);
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