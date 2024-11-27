using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.MagGraph.Model;

namespace T3.Editor.Gui.MagGraph.Ui;

internal sealed partial class MagGraphCanvas
{
    private void DrawConnection(MagGraphConnection connection, ImDrawListPtr drawList)
    {
        if (connection.Style == MagGraphConnection.ConnectionStyles.Unknown)
            return;

        var type = connection.Type;

        // if (!TypeUiRegistry.TryGetPropertiesForType(type, out var typeUiProperties))
        //     return;

        var typeUiProperties = TypeUiRegistry.GetPropertiesForType(type);

        var anchorSize = 4 * CanvasScale;
        var typeColor = typeUiProperties.Color;
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
                    // break;
                    //case MagGraphConnection.ConnectionStyles.RightToLeft:
                    //var hoverPositionOnLine = Vector2.Zero;
                    // var isHovering = ArcConnection.Draw( Scale,
                    //                                      new ImRect(sourcePosOnScreen, sourcePosOnScreen + new Vector2(10, 10)),
                    //                                     sourcePosOnScreen,
                    //                                     ImRect.RectWithSize(
                    //                                                         TransformPosition(connection.TargetItem.PosOnCanvas),
                    //                                                         TransformDirection(connection.TargetItem.Size)),
                    //                                     targetPosOnScreen,
                    //                                     typeColor,
                    //                                     2,
                    //                                     ref hoverPositionOnLine);

                    // const float minDistanceToTargetSocket = 10;
                    // if (isHovering && Vector2.Distance(hoverPositionOnLine, TargetPosition) > minDistanceToTargetSocket
                    //                && Vector2.Distance(hoverPositionOnLine, SourcePosition) > minDistanceToTargetSocket)
                    // {
                    //     ConnectionSplitHelper.RegisterAsPotentialSplit(Connection, ColorForType, hoverPositionOnLine);
                    // }                        
                    //
                    drawList.AddBezierCubic(sourcePosOnScreen,
                                            sourcePosOnScreen + new Vector2(d, 0),
                                            targetPosOnScreen - new Vector2(d, 0),
                                            targetPosOnScreen,
                                            typeColor.Fade(0.6f),
                                            2);
                    
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