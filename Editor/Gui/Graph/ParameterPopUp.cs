using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;

namespace T3.Editor.Gui.Graph;

internal static class ParameterPopUp
{
    public static void OpenParameterPopUp(SymbolChildUi childUi, Instance instance, SymbolChildUi.CustomUiResult customUiResult, ImRect nodeScreenRect)
    {
        var activatedWithLeftMouse = ImGui.IsItemHovered()
                                     && ImGui.IsMouseReleased(ImGuiMouseButton.Left)
                                     && ImGui.GetMouseDragDelta(ImGuiMouseButton.Left, 0).Length() < UserSettings.Config.ClickThreshold
                                     && !ParameterWindow.IsAnyInstanceVisible()
                                     && !ImGui.GetIO().KeyShift; // allow double click to open

        var activatedWithMiddleMouse = ImGui.IsItemHovered()
                                       && ImGui.IsMouseReleased(ImGuiMouseButton.Middle)
                                       && ImGui.GetMouseDragDelta(ImGuiMouseButton.Middle, 0).Length() < UserSettings.Config.ClickThreshold;

        var activationRequested = NodeIdRequestedForParameterWindowActivation == instance.SymbolChildId
                                  && !ParameterWindow.IsAnyInstanceVisible();

        if ((activatedWithLeftMouse || activatedWithMiddleMouse || activationRequested)
            && string.IsNullOrEmpty(FrameStats.Current.OpenedPopUpName)
            && (customUiResult & SymbolChildUi.CustomUiResult.PreventOpenParameterPopUp) == 0
            && FrameStats.Last.OpenedPopUpName != CurrentOpenedPopUpName)
        {
            NodeIdRequestedForParameterWindowActivation = Guid.Empty;
            NodeSelection.SetSelectionToChildUi(childUi, instance);
            _selectedInstance = instance;

            var screenPos = new Vector2(nodeScreenRect.Min.X + 5, nodeScreenRect.Max.Y + 5);
            ImGui.SetNextWindowPos(screenPos);
            ImGui.OpenPopup(CurrentOpenedPopUpName);
        }
    }

    public static void DrawParameterPopUp(Instance instance, bool justOpenedChild, SymbolUi symbolUi_)
    {
        // if (!justOpenedChild)
        //     return;

        ImGui.SetNextWindowSizeConstraints(new Vector2(280, 40), new Vector2(280, 320));
        if (ImGui.BeginPopup(CurrentOpenedPopUpName))
        {
            var symbolUi = SymbolUiRegistry.Entries[_selectedInstance.Symbol.Id];
            var compositionSymbolUi = SymbolUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];
            var symbolChildUi = compositionSymbolUi.ChildUis.Single(symbolChildUi2 => symbolChildUi2.Id == _selectedInstance.SymbolChildId);
            //var symbolUi = SymbolUiRegistry.Entries[_selectedInstance.Parent]

            FrameStats.Current.OpenedPopUpName = CurrentOpenedPopUpName;
            ImGui.PushFont(Fonts.FontSmall);
            ParameterWindow.DrawParameters(_selectedInstance, symbolUi, symbolChildUi, compositionSymbolUi);
            ImGui.PopFont();
            ImGui.EndPopup();
        }
    }

    public static Instance _selectedInstance;

    public static readonly string CurrentOpenedPopUpName = "parameterContextPopup";
    public static Guid NodeIdRequestedForParameterWindowActivation;
}