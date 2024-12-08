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
        BestMatchLastFrame = _bestMatchYetForCurrentFrame;

        if (BestMatchLastFrame != null)
        {
            var time = ImGui.GetTime();
            if (_hoverStartTime < 0)
                _hoverStartTime = time;

            var hoverDuration = time - _hoverStartTime;
            var radius = EaseFunctions.EaseOutElastic((float)hoverDuration) * 4;
            var drawList = ImGui.GetForegroundDrawList();

            drawList.AddCircleFilled(BestMatchLastFrame.PositionOnScreen, radius, BestMatchLastFrame.Color, 30);

            var buttonMin = _mousePosition - Vector2.One * radius / 2;
            ImGui.SetCursorScreenPos(buttonMin);

            if (ImGui.InvisibleButton("splitMe", Vector2.One * radius))
            {
                var posOnScreen = context.Canvas.InverseTransformPositionFloat(BestMatchLastFrame.PositionOnScreen)
                                  - new Vector2(SymbolUi.Child.DefaultOpSize.X * 0.25f,
                                                SymbolUi.Child.DefaultOpSize.Y * 0.5f);

                // TODO: Implement correctly
                // ConnectionMaker.SplitConnectionWithSymbolBrowser(window, window.CompositionOp!.Symbol,
                //                                                  _bestMatchYetForCurrentFrame.Connection,
                //                                                  posOnScreen);
            }

            ImGui.BeginTooltip();
            {
                var connection = BestMatchLastFrame.Connection;
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
        else
        {
            _hoverStartTime = -1;
        }

        _bestMatchYetForCurrentFrame = null;
        _bestMatchDistance = float.PositiveInfinity;
    }

    internal static bool IsHovered(MagGraphConnection connection)
    {
        return BestMatchLastFrame != null && BestMatchLastFrame.Connection == connection;
    }
    
    public static void ResetSnapping()
    {
        BestMatchLastFrame = null;
    }

    public static void RegisterAsPotentialSplit(MagGraphConnection mcConnection, Color color, Vector2 position, float normalizedPosition)
    {
        var distance = Vector2.Distance(position, _mousePosition);
        if (distance > SnapDistance || distance > _bestMatchDistance)
        {
            return;
        }

        _bestMatchYetForCurrentFrame = new PotentialConnectionSplit(position, mcConnection, color);
        _bestMatchDistance = distance;
    }

    private static readonly ImageOutputCanvas _imageCanvasForTooltips = new() { DisableDamping = true };
    private static readonly EvaluationContext _evaluationContext = new();

    public static PotentialConnectionSplit? BestMatchLastFrame;
    private static PotentialConnectionSplit? _bestMatchYetForCurrentFrame;
    private static float _bestMatchDistance = float.PositiveInfinity;
    private const int SnapDistance = 50;
    private static Vector2 _mousePosition;
    private static double _hoverStartTime = -1;

    public sealed record PotentialConnectionSplit(Vector2 PositionOnScreen, MagGraphConnection Connection, Color Color);
}