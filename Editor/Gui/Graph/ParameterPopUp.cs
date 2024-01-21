using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.Gui.Windows.Layouts;
using T3.Editor.Gui.Windows.Variations;
using T3.Editor.UiModel;

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
            && (customUiResult & SymbolChildUi.CustomUiResult.PreventOpenParameterPopUp) == 0
           )
        {
            Open(instance);
        }
    }

    public static void Open(Instance instance)
    {
        _selectedInstance = instance;
        _graphCanvas = GraphCanvas.Current;

        NodeIdRequestedForParameterWindowActivation = Guid.Empty;

        _isOpen = true;
        _lastRequiredHeight = 0;
        _focusDelayCount = 3;
    }

    public static void DrawParameterPopUp(GraphWindow graphWindow)
    {
        if (!_isOpen || _selectedInstance == null || _graphCanvas == null)
            return;

        var symbolUi = SymbolUiRegistry.Entries[_selectedInstance.Symbol.Id];
        var compositionSymbolUi = SymbolUiRegistry.Entries[_graphCanvas.CompositionOp.Symbol.Id];
        var symbolChildUi = compositionSymbolUi.ChildUis.SingleOrDefault(symbolChildUi2 => symbolChildUi2.Id == _selectedInstance.SymbolChildId);
        if (symbolChildUi == null)
        {
            Close();
            return;
        }

        if (!NodeSelection.IsAnythingSelected())
        {
            Close();
            return;
        }

        var nodeScreenRect = _graphCanvas.TransformRect(ImRect.RectWithSize(symbolChildUi.PosOnCanvas, symbolChildUi.Size));
        var screenPos = new Vector2(nodeScreenRect.Min.X + 5, nodeScreenRect.Max.Y + 5);
        var height = _lastRequiredHeight.Clamp(MinHeight, MaxHeight);
        ImGui.SetNextWindowPos(screenPos);

        var preventTabbingIntoUnfocusedStringInputs = ImGui.IsAnyItemActive() ? ImGuiWindowFlags.None : ImGuiWindowFlags.NoNavInputs;
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        if (ImGui.BeginChild("Popup",
                             new Vector2(280, height),
                             true,
                             preventTabbingIntoUnfocusedStringInputs
                             | ImGuiWindowFlags.NoScrollbar))
        {
            if (ImGui.IsKeyDown(ImGuiKey.Escape))
            {
                Close();
            }

            ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetWindowPos(),
                                                    ImGui.GetWindowPos() + ImGui.GetWindowSize(),
                                                    UiColors.BackgroundFull);

            FormInputs.SetIndent(20);

            if (_focusDelayCount >= 0)
            {
                ImGui.SetWindowFocus();
                _focusDelayCount--;
            }
            else if (!ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows))
            {
                Close();
            }

            // Toolbar
            {
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4,4) * T3Ui.UiScaleFactor);
                ImGui.SetCursorPos( new Vector2(5,5));
                CustomComponents.AddSegmentedIconButton(ref _viewMode, _modeIcons);
                ImGui.SameLine(0, 20);

                var isPinned = _selectedInstance == graphWindow.GraphImageBackground.OutputInstance;
                if (CustomComponents.DrawIconToggle("enabled", Icon.PlayOutput, ref isPinned))
                {
                    if (isPinned)
                        graphWindow.SetBackgroundOutput(_selectedInstance);
                }
                ImGui.PopStyleVar();
            }

            // Content
            switch (_viewMode)
            {
                case ViewModes.Parameters:
                    FrameStats.Current.OpenedPopUpName = ParameterPopUpName;
                    ImGui.PushFont(Fonts.FontSmall);
                    ParameterWindow.DrawParameters(_selectedInstance, symbolUi, symbolChildUi, compositionSymbolUi, hideNonEssentials: true);
                    ImGui.PopFont();
                    break;
                case ViewModes.Presets:
                    var allWindows = WindowManager.GetAllWindows();
                    foreach (var w in allWindows)
                    {
                        if (w is not VariationsWindow variationsWindow)
                            continue;

                        variationsWindow.DrawWindowContent(hideHeader: true);
                    }

                    break;
                case ViewModes.Help:
                    FormInputs.AddVerticalSpace();
                    ImGui.PushFont(Fonts.FontNormal);
                    ImGui.SetCursorPosX(10);
                    ImGui.TextUnformatted(symbolUi.Symbol.Name);
                    ImGui.PopFont();
                    FormInputs.AddVerticalSpace();
                    OperatorHelp.DrawDescription(symbolUi);
                    FormInputs.AddVerticalSpace();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            _lastRequiredHeight = ImGui.GetCursorPosY();
        }

        ImGui.EndChild();
        ImGui.PopStyleVar();
    }

    private static void Close()
    {
        _isOpen = false;
    }

    private enum ViewModes
    {
        Parameters,
        Presets,
        Help,
    }

    private static readonly List<Icon> _modeIcons = new()
                                                        {
                                                            Icon.ParamsList,
                                                            Icon.Presets,
                                                            Icon.HelpOutline
                                                        };

    private static float _lastRequiredHeight;
    private const float MaxHeight = 280;
    private const float MinHeight = 50;

    private static bool _isOpen;
    private static int _focusDelayCount;

    private static GraphCanvas _graphCanvas;
    private static ViewModes _viewMode = ViewModes.Parameters;
    private static Instance _selectedInstance;
    private const string ParameterPopUpName = "parameterContextPopup";
    public static Guid NodeIdRequestedForParameterWindowActivation;
}