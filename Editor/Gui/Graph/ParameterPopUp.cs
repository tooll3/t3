using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.Gui.Windows.Layouts;
using T3.Editor.Gui.Windows.Variations;

namespace T3.Editor.Gui.Graph;

internal static class ParameterPopUp
{
    public static void HandleOpenParameterPopUp(SymbolChildUi childUi, Instance instance, SymbolChildUi.CustomUiResult customUiResult, ImRect nodeScreenRect)
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
            && FrameStats.Last.OpenedPopUpName != ParameterPopUpName)
        {
            _selectedInstance = instance;
            _graphCanvas = GraphCanvas.Current;
        }
    }

    private static bool _isOpen;
    private static GraphCanvas _graphCanvas;

    
    public static void DrawParameterPopUp()
    {
        if (_selectedInstance == null || _graphCanvas == null)
            return;
        
        var symbolUi = SymbolUiRegistry.Entries[_selectedInstance.Symbol.Id];
        var compositionSymbolUi = SymbolUiRegistry.Entries[_graphCanvas.CompositionOp.Symbol.Id];
        var symbolChildUi = compositionSymbolUi.ChildUis.SingleOrDefault(symbolChildUi2 => symbolChildUi2.Id == _selectedInstance.SymbolChildId);
        if (symbolChildUi == null)
        {
            Close();
            return;
        }
        
        if (!_isOpen)
        {
            NodeIdRequestedForParameterWindowActivation = Guid.Empty;
            NodeSelection.SetSelectionToChildUi(symbolChildUi, _selectedInstance);

            var nodeScreenRect = _graphCanvas.TransformRect(ImRect.RectWithSize(symbolChildUi.PosOnCanvas, symbolChildUi.Size));
            
            var screenPos = new Vector2(nodeScreenRect.Min.X + 5, nodeScreenRect.Max.Y + 5);
            ImGui.SetNextWindowPos(screenPos);
            ImGui.OpenPopup(ParameterPopUpName);
            _isOpen = true;
        }
        
        ImGui.SetNextWindowSizeConstraints(new Vector2(280, 140), new Vector2(280, 320));
        if (ImGui.BeginPopup(ParameterPopUpName,  ImGuiWindowFlags.NoMove))
        {
            var io = ImGui.GetIO();
            
            if (ImGui.IsKeyDown(ImGuiKey.Escape))
            {
                ImGui.CloseCurrentPopup();
            }

            if (ImGui.IsKeyPressed((ImGuiKey)Key.CursorLeft))
            {
                // TODO: implement quick node selection
                //Log.Debug("Left!");
            }

            FormInputs.SetIndent(20);

            CustomComponents.AddSegmentedIconButton(ref _viewMode, new List<Icon>() { Icon.ParamsList, Icon.Presets, Icon.HelpOutline });
            //FormInputs.AddSegmentedButton(ref _viewMode, "");
            switch (_viewMode)
            {
                case ViewModes.Parameters:
                    FrameStats.Current.OpenedPopUpName = ParameterPopUpName;
                    ImGui.PushFont(Fonts.FontSmall);
                    ParameterWindow.DrawParameters(_selectedInstance, symbolUi, symbolChildUi, compositionSymbolUi);
                    ImGui.PopFont();
                    break;
                case ViewModes.Presets:
                    var allWindows = WindowManager.GetAllWindows();
                    foreach (var w in allWindows)
                    {
                        if (w is not VariationsWindow variationsWindow)
                            continue;
                        
                        variationsWindow.DrawWindowContent(hideHeader:true);
                    }
                    break;
                case ViewModes.Help:
                    FormInputs.AddVerticalSpace(10);
                    ImGui.PushFont(Fonts.FontNormal);
                    ImGui.SetCursorPosX(10);
                    ImGui.TextUnformatted(symbolUi.Symbol.Name);
                    ImGui.PopFont();
                    FormInputs.AddVerticalSpace(10);
                    ParameterWindow.DrawDescription(symbolUi);
                    FormInputs.AddVerticalSpace(10);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
            ImGui.EndPopup();
        }
        else
        {
            Close();
        }
    }

    private static void Close()
    {
        _selectedInstance = null;
        _isOpen = false;
    }

    private enum ViewModes
    {
        Parameters,
        Presets,
        Help,
    }

    private static ViewModes _viewMode = ViewModes.Parameters;
    //private static PresetCanvas _presetCanvas = new();
    private static Instance _selectedInstance;
    public static readonly string ParameterPopUpName = "parameterContextPopup";
    public static Guid NodeIdRequestedForParameterWindowActivation;
}