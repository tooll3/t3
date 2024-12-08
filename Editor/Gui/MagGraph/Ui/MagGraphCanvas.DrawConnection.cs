using ImGuiNET;
using T3.Core.Utils;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.MagGraph.Model;
using T3.Editor.Gui.MagGraph.States;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using Color = T3.Core.DataTypes.Vector.Color;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.MagGraph.Ui;

internal sealed partial class MagGraphCanvas
{
    private void DrawConnection(MagGraphConnection connection, ImDrawListPtr drawList, GraphUiContext context)
    {
        if (connection.Style == MagGraphConnection.ConnectionStyles.Unknown)
            return;

        var type = connection.Type;

        // if (!TypeUiRegistry.TryGetPropertiesForType(type, out var typeUiProperties))
        //     return;

        var isSelected = context.Selector.IsSelected(connection.SourceItem) ||
                         context.Selector.IsSelected(connection.TargetItem);
        
        var typeUiProperties = TypeUiRegistry.GetPropertiesForType(type);

        var anchorSize = 4 * CanvasScale;
        var idleFadeProgress = MathUtils.RemapAndClamp(connection.SourceOutput.DirtyFlag.FramesSinceLastUpdate, 0, 100, 1, 0f);
        
        var color =  typeUiProperties.Color;
        var selectedColor = isSelected ?  ColorVariations.OperatorLabel.Apply(color)
                                : ColorVariations.ConnectionLines.Apply(color);
        var typeColor = ColorVariations.ConnectionLines.Apply(selectedColor).Fade(MathUtils.Lerp(0.5f, 1, idleFadeProgress));
        
        var sourcePosOnScreen = TransformPosition(connection.SourcePos);
        var targetPosOnScreen = TransformPosition(connection.TargetPos);


        if (connection.IsSnapped)
        {
            switch (connection.Style)
            {
                case MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedHorizontal:
                {
                    var isPotentialSplitTarget = _context.ItemMovement.SplitInsertionPoints.Count > 0
                                                 && _context.ItemMovement.SplitInsertionPoints
                                                            .Any(x
                                                                     => x.Direction == MagGraphItem.Directions.Horizontal
                                                                        && x.Type == type);
                    if (isPotentialSplitTarget)
                    {
                        var extend = new Vector2(0, MagGraphItem.GridSize.Y * CanvasScale * 0.4f);

                        drawList.AddRectFilled(
                                               sourcePosOnScreen - extend + new Vector2(-1, 0),
                                               sourcePosOnScreen + extend + new Vector2(0, 0),
                                               typeColor.Fade(Blink)
                                              );
                    }

                    drawList.AddCircleFilled(sourcePosOnScreen, anchorSize * 1.6f, typeColor, 3);
                    break;
                }
                
                    
                case MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedVertical:
                {
                    var isPotentialSplitTarget = _context.ItemMovement.SplitInsertionPoints.Count > 0
                                                 && _context.ItemMovement.SplitInsertionPoints
                                                            .Any(x
                                                                     => x.Direction == MagGraphItem.Directions.Vertical
                                                                        && x.Type == type);
                    if (isPotentialSplitTarget)
                    {
                        var extend = new Vector2(MagGraphItem.GridSize.X * CanvasScale * 0.4f, 0);

                        drawList.AddRectFilled(
                                               sourcePosOnScreen - extend + new Vector2(0, -1),
                                               sourcePosOnScreen + extend + new Vector2(0, 0),
                                               typeColor.Fade(Blink)
                                              );
                    }

                    drawList.AddTriangleFilled(
                                               sourcePosOnScreen + new Vector2(-1, -1) * CanvasScale * 5,
                                               sourcePosOnScreen + new Vector2(1, -1) * CanvasScale * 5,
                                               sourcePosOnScreen + new Vector2(0, 1) * CanvasScale * 5,
                                               typeColor);
                    break;
                }
                case MagGraphConnection.ConnectionStyles.MainOutToInputSnappedHorizontal:
                    drawList.AddCircleFilled(sourcePosOnScreen, anchorSize * 1.6f, typeColor, 3);
                    break;
                
                case MagGraphConnection.ConnectionStyles.AdditionalOutToMainInputSnappedVertical:
                    drawList.AddCircleFilled(sourcePosOnScreen, anchorSize * 1.6f, Color.Red, 3);
                    break;
            }
        }
        else
        {
            var d = Vector2.Distance(sourcePosOnScreen, targetPosOnScreen) / 2;

            switch (connection.Style)
            {
                case MagGraphConnection.ConnectionStyles.BottomToTop:
                    drawList.AddBezierCubic(sourcePosOnScreen,
                                            sourcePosOnScreen + new Vector2(0, d),
                                            targetPosOnScreen - new Vector2(0, d),
                                            targetPosOnScreen,
                                            typeColor.Fade(0.6f),
                                            2);
                    
                    drawList.AddTriangleFilled(
                                               targetPosOnScreen + new Vector2(-1, -1 + 1) * CanvasScale * 3,
                                               targetPosOnScreen + new Vector2(1, -1 + 1) * CanvasScale * 3,
                                               targetPosOnScreen + new Vector2(0, 1 + 1) * CanvasScale * 3,
                                               typeColor);                    
                    break;
                case MagGraphConnection.ConnectionStyles.BottomToLeft:
                    drawList.AddBezierCubic(sourcePosOnScreen,
                                            sourcePosOnScreen + new Vector2(0, d),
                                            targetPosOnScreen - new Vector2(d, 0),
                                            targetPosOnScreen,
                                            typeColor.Fade(0.6f),
                                            2);
                    
                    break;
                case MagGraphConnection.ConnectionStyles.RightToTop:
                    drawList.AddBezierCubic(sourcePosOnScreen,
                                            sourcePosOnScreen + new Vector2(d, 0),
                                            targetPosOnScreen - new Vector2(0, d),
                                            targetPosOnScreen,
                                            typeColor.Fade(0.6f),
                                            2);
                    
                    drawList.AddTriangleFilled(
                                               sourcePosOnScreen + new Vector2(-1, -1) * CanvasScale * 5,
                                               sourcePosOnScreen + new Vector2(1, -1) * CanvasScale * 5,
                                               sourcePosOnScreen + new Vector2(0, 1) * CanvasScale * 5,
                                               typeColor);
                    break;
                
                
                case MagGraphConnection.ConnectionStyles.RightToLeft:
                    
                    //Vector2 hoverPositionOnLine = ImGui.GetMousePos();
                    // var isHovering = ArcConnection.Draw(
                    //                                     Vector2.One *  CanvasScale,
                    //                    TransformRect(connection.SourceItem.Area), 
                    //                    sourcePosOnScreen, 
                    //                    TransformRect( connection.TargetItem.VerticalStackArea), 
                    //                    targetPosOnScreen, 
                    //                    typeColor,
                    //                    1.5f,
                    //                    ref hoverPositionOnLine);
                    //
                    // const float minDistanceToTargetSocket = 10;
                    // if (isHovering && Vector2.Distance(hoverPositionOnLine, targetPosOnScreen) > minDistanceToTargetSocket
                    //                && Vector2.Distance(hoverPositionOnLine, sourcePosOnScreen) > minDistanceToTargetSocket)
                    // {
                    //     ConnectionSplitHelper.RegisterAsPotentialSplit(connection.AsSymbolConnection(), typeColor, hoverPositionOnLine);
                    // }

                    var isHovering2 = GraphConnectionDrawer.DrawConnection(CanvasScale,
                                                         TransformRect(connection.SourceItem.Area),
                                                         sourcePosOnScreen,
                                                         TransformRect(connection.TargetItem.VerticalStackArea),
                                                         targetPosOnScreen,
                                                         typeColor,
                                                         MathUtils.Lerp(1f,2f,idleFadeProgress) + (isSelected ? 1:0),
                                                         out var hoverPositionOnLine,
                                                                           out var normalizedHoverPos);
                    
                    if(isHovering2)
                        Log.Debug("is hovering " + connection + " " + normalizedHoverPos);
                    
                    drawList.AddCircleFilled(targetPosOnScreen + new Vector2(3 * CanvasScale,0) , anchorSize * 1.2f, typeColor, 3);

                    break;
                case MagGraphConnection.ConnectionStyles.Unknown:
                    break;
                case MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedHorizontal:
                    break;
                case MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedVertical:
                    break;
                case MagGraphConnection.ConnectionStyles.MainOutToInputSnappedHorizontal:
                    break;
                case MagGraphConnection.ConnectionStyles.AdditionalOutToMainInputSnappedVertical:
                    break;
            }
        }
    }
}