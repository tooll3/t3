#nullable enable
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Interaction.TransformGizmos;
using T3.Editor.Gui.MagGraph.Model;
using T3.Editor.Gui.MagGraph.States;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.UiModel;
using Color = T3.Core.DataTypes.Vector.Color;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.MagGraph.Interaction;

/// <summary>
/// This is a derived version of ConnectionSplit helper. That older version was tightly coupled with the
/// legacy graph window.
///
/// TODO:
/// - support hovering multiple connections
/// - indicate input / center / output region on the connection
/// 
/// </summary>
internal static class ConnectionHovering
{
    internal static void PrepareNewFrame(GraphUiContext context)
    {
        _mousePosition = ImGui.GetMousePos();

        // Swap lists
        (_lastConnectionHovers, _connectionHoversForCurrentFrame) = (_connectionHoversForCurrentFrame, _lastConnectionHovers);
        _connectionHoversForCurrentFrame.Clear();

        if (_lastConnectionHovers.Count == 0)
        {
            StopHover();
            return;
        }

        var firstHover = _lastConnectionHovers[0];
        var time = ImGui.GetTime();
        if (_hoverStartTime < 0)
            _hoverStartTime = time;

        var hoverDuration = time - _hoverStartTime;
        var hoverIndicatorRadius = EaseFunctions.EaseOutElastic((float)hoverDuration) * 9;
        var drawList = ImGui.GetForegroundDrawList();

        drawList.AddCircleFilled(firstHover.PositionOnScreen, hoverIndicatorRadius, firstHover.Color, 30);

        // For merged lines with matching types and line regions, we can offer multi drag...
        var bounds = ImRect.RectWithSize(firstHover.PositionOnScreen, Vector2.Zero);
        var firstType = firstHover.Connection.Type;
        var firstOutput = firstHover.Connection.SourceOutput;
        var typesMatch = true;
        var region = GetLineRegion(firstHover);

        for (var index = 1; index < _lastConnectionHovers.Count; index++)
        {
            var h = _lastConnectionHovers[index];
            bounds.Add(h.PositionOnScreen);
            if (h.Connection.Type != firstType)
                typesMatch = false;

            if (region != GetLineRegion(h))
                region = LineRegions.Undefined;

            if (firstOutput != h.Connection.SourceOutput)
                firstOutput = null;
        }

        var tooLarge = bounds.GetHeight() > 2 || bounds.GetWidth() > 2;

        if (!tooLarge && typesMatch && region != LineRegions.Undefined && firstOutput != null)
        {
            // We only draw first indicator, because hover points fall together closely...
            drawList.AddCircleFilled(firstHover.PositionOnScreen, hoverIndicatorRadius, firstHover.Color, 12);
            
            // Prepare disconnecting from input slot...
            if (region == LineRegions.End)
            {
                if (_lastConnectionHovers.Count == 1)
                {
                    var inputPosInScreen = context.Canvas.TransformPosition(firstHover.Connection.TargetPos);
                    drawList.AddCircle(inputPosInScreen, hoverIndicatorRadius, firstHover.Color, 24);
                }
            }
            else if (region == LineRegions.Beginning)
            {
                var outputPosOnScreen = context.Canvas.TransformPosition(firstHover.Connection.SourcePos);
                drawList.AddCircle(outputPosOnScreen, hoverIndicatorRadius, firstHover.Color, 24);
            }

            ImGui.BeginTooltip();
            ImGui.TextUnformatted("Click to insert operator or\ndrag to disconnect...");
            ImGui.EndTooltip();
        }
        else if (firstOutput != null)
        {
            DrawTooltipForSingleOutput(context, firstHover, drawList);
            // Inconsistent connection types...
        }

        // TODO: Implement splitting
        // var buttonMin = _mousePosition - Vector2.One * radius / 2;
        // ImGui.SetCursorScreenPos(buttonMin);
        // if (ImGui.InvisibleButton("splitMe", Vector2.One * radius))
        // {
        //     var posOnScreen = context.Canvas.InverseTransformPositionFloat(bestMatchLastFrame.PositionOnScreen)
        //                       - new Vector2(SymbolUi.Child.DefaultOpSize.X * 0.25f,
        //                                     SymbolUi.Child.DefaultOpSize.Y * 0.5f);
        //
        //     // TODO: Implement correctly
        //     // ConnectionMaker.SplitConnectionWithSymbolBrowser(window, window.CompositionOp!.Symbol,
        //     //                                                  _bestMatchYetForCurrentFrame.Connection,
        //     //                                                  posOnScreen);
        // }
        else
        {
            StopHover();
        }
        _bestSnapSplitDistance = float.PositiveInfinity;
    }

    private static void DrawTooltipForSingleOutput(GraphUiContext context, HoverPoint bestMatchLastFrame, ImDrawListPtr drawList)
    {
        ImGui.BeginTooltip();
        {
            var connection = bestMatchLastFrame.Connection;
            var sourceOpInstance = connection.SourceItem.Instance;
            var outputSlot = connection.SourceOutput;

            var targetOp = connection.TargetItem.Instance;

            if (connection.SourceItem.SymbolUi != null
                && sourceOpInstance != null
                && outputSlot != null
                && targetOp != null
                && connection.TargetItem.SymbolUi != null)
            {
                var width = 160f;
                ImGui.SetNextWindowSizeConstraints(new Vector2(200, 200 * 9 / 16f), new Vector2(200, 200 * 9 / 16f));

                var sourceOpUi = sourceOpInstance.GetSymbolUi();
                var sourceOutputUi = sourceOpUi.OutputUis[connection.SourceOutput.Id];
                ImGui.BeginChild("thumbnail", new Vector2(width, width * 9 / 16f));
                {
                    TransformGizmoHandling.SetDrawList(drawList);
                    _imageCanvasForTooltips.Update();
                    _imageCanvasForTooltips.SetAsCurrent();

                    var outputUi = connection.SourceItem.SymbolUi.OutputUis[outputSlot.Id];
                    _evaluationContext.Reset();
                    _evaluationContext.RequestedResolution = new Int2(1280 / 2, 720 / 2);
                    outputUi.DrawValue(outputSlot, _evaluationContext, recompute: UserSettings.Config.HoverMode == GraphHoverModes.Live);

                    _imageCanvasForTooltips.Deactivate();
                    TransformGizmoHandling.RestoreDrawList();
                }
                ImGui.EndChild();
                ImGui.PushFont(Fonts.FontSmall);
                var connectionSource = sourceOpUi.Symbol.Name + "." + sourceOutputUi.OutputDefinition.Name;
                ImGui.TextColored(UiColors.TextMuted, connectionSource);
                var symbolChildInput = connection.TargetItem.SymbolUi.InputUis[connection.TargetInput.Id];

                ImGui.TextUnformatted(connection.Type.Name);
                ImGui.SameLine();
                var inputIndex = connection.MultiInputIndex > 0 ? "[" + connection.MultiInputIndex + "]" : String.Empty;
                var connectionTarget = "--> " + targetOp.Symbol.Name + "." + symbolChildInput.InputDefinition.Name + inputIndex;
                ImGui.TextColored(UiColors.TextMuted, connectionTarget);
                ImGui.PopFont();

                var nodeSelection = context.Selector;
                nodeSelection.HoveredIds.Add(targetOp.SymbolChildId);
                nodeSelection.HoveredIds.Add(connection.SourceItem.Id);
            }
        }
        ImGui.EndTooltip();
    }

    private static void StopHover()
    {
        _hoverStartTime = -1;
    }

    internal enum LineRegions
    {
        Undefined,
        Beginning,
        Center,
        End,
    }

    internal static LineRegions GetLineRegion(HoverPoint hoverPoint)
    {
        const float threshold = 0.3f;
        return hoverPoint.NormalizedDistanceOnLine switch
                   {
                       < threshold     => LineRegions.Beginning,
                       > 1 - threshold => LineRegions.End,
                       _               => LineRegions.Center
                   };
    }

    internal static bool IsHovered(MagGraphConnection connection)
    {
        foreach (var h in _lastConnectionHovers)
        {
            if (h.Connection == connection)
                return true;
        }

        return false;
    }

    // TODO: Implement for dragging.
    public static void RegisterAsPotentialSplit(MagGraphConnection mcConnection, Color color, Vector2 position, float normalizedPosition)
    {
        var distance = Vector2.Distance(position, _mousePosition);
        if (distance > SnapDistance || distance > _bestSnapSplitDistance)
        {
            return;
        }

        _connectionHoversForCurrentFrame.Add(new HoverPoint(position, normalizedPosition, mcConnection, color));
    }

    private static readonly ImageOutputCanvas _imageCanvasForTooltips = new() { DisableDamping = true };
    private static readonly EvaluationContext _evaluationContext = new();

    private static List<HoverPoint> _lastConnectionHovers = [];
    private static List<HoverPoint> _connectionHoversForCurrentFrame = []; // deferred, because hovered is computed during draw.

    private static float _bestSnapSplitDistance = float.PositiveInfinity;
    private const int SnapDistance = 50;
    private static Vector2 _mousePosition;
    private static double _hoverStartTime = -1;

    public sealed record HoverPoint(
        Vector2 PositionOnScreen,
        float NormalizedDistanceOnLine,
        MagGraphConnection Connection,
        Color Color);
}