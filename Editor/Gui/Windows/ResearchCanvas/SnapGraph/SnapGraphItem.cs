using System;
using System.Collections.Generic;
using System.Numerics;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.OutputUi;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

// ReSharper disable UseWithExpressionToCopyStruct

namespace T3.Editor.Gui.Windows.ResearchCanvas.SnapGraph;

public class SnapGraphItem : ISelectableCanvasObject
{
    public Guid Id { get; init; }
    public Type PrimaryType = typeof(float);
    public Vector2 PosOnCanvas { get => SymbolChildUi.PosOnCanvas; set => SymbolChildUi.PosOnCanvas = value; }
    public Vector2 Size { get; set; }
    public bool IsSelected => NodeSelection.IsNodeSelected(SymbolChildUi);
    public SnapGroup SnapGroup;
    public float UnitHeight => InputLines.Length + OutputLines.Length + 1;

    public override string ToString()
    {
        return SymbolChild.ReadableName;
    }

    public SymbolUi SymbolUi;
    public SymbolChild SymbolChild;
    public SymbolChildUi SymbolChildUi;
    public Instance Instance;

    public InputLine[] InputLines;
    public OutputLine[] OutputLines;

    public struct InputLine
    {
        public SnapGraphItem TargetItem;
        public IInputSlot Input;
        public IInputUi InputUi;
        public bool IsPrimary;
        public int VisibleIndex;
        public SnapGraphConnection Connection;
        public int MultiInputIndex;
    }

    public struct OutputLine
    {
        public SnapGraphItem SourceItem;
        public ISlot Output;
        public IOutputUi OutputUi;
        public bool IsPrimary;
        public int VisibleIndex;
        public int OutputIndex;
        public List<SnapGraphConnection> Connections;
    }

    public struct AnchorPoint
    {
        public Vector2 PositionOnCanvas;
        public Directions Direction;
        public Type ConnectionType;
        public int ConnectionHash;
        public Guid SlotId;

        public bool IsConnected => ConnectionHash != 0;

        public float GetSnapDistance(AnchorPoint other)
        {
            if (other.ConnectionType != ConnectionType 
                || other.Direction != Direction 
                || other.ConnectionHash != ConnectionHash // FIXME
                )
                return float.PositiveInfinity;

            return Vector2.Distance(other.PositionOnCanvas, PositionOnCanvas);
        }
    }

    public enum Directions
    {
        Horizontal,
        Vertical,
    }

    public const float Width = 80;
    public const float WidthHalf = Width/2;
    public const float LineHeight = 20;
    public const float LineHeightHalf = LineHeight/2;
    public static readonly Vector2 GridSize = new Vector2(Width, LineHeight);

    public bool IsDragged; // FIXME: Implement

    public ImRect Area => ImRect.RectWithSize(PosOnCanvas, Size);

    public static ImRect GetGroupBoundingBox(IEnumerable<SnapGraphItem> items)
    {
        ImRect extend = default;
        var index2 = 0;
        foreach (var item in items)
        {
            if (index2 == 0)
            {
                extend = item.Area;
            }
            else
            {
                extend.Add(item.Area);
            }

            index2++;
        }

        return extend;
    }

    /// <summary>
    /// Get input anchors with current position and orientation.
    /// </summary>
    /// <remarks>
    /// Using an Enumerable interface here is bad, because it creates a lot of allocations.
    /// In the long term, this should be cached.
    /// </remarks>
    public IEnumerable<AnchorPoint> GetInputAnchors()
    {
        if (InputLines.Length == 0)
            yield break;

        yield return new AnchorPoint
                         {
                             PositionOnCanvas = new Vector2(WidthHalf, 0) + PosOnCanvas,
                             Direction = Directions.Vertical,
                             ConnectionType = InputLines[0].Input.ValueType,
                             ConnectionHash = InputLines[0].Connection?.ConnectionHash ?? 0,
                             SlotId = InputLines[0].Input.Id,
                         };

        foreach (var il in InputLines)
        {
            yield return new AnchorPoint
                             {
                                 PositionOnCanvas = new Vector2(0, (0.5f + il.VisibleIndex) * LineHeight) + PosOnCanvas,
                                 Direction = Directions.Horizontal,
                                 ConnectionType = il.Input.ValueType,
                                 ConnectionHash = il.Connection?.ConnectionHash ?? 0,
                                 SlotId = il.Input.Id,
                             };
        }
    }

    public IEnumerable<AnchorPoint> GetOutputAnchors()
    {
        if (OutputLines.Length == 0)
            yield break;

        yield return new AnchorPoint
                         {
                             PositionOnCanvas = new Vector2(WidthHalf, Size.Y) + PosOnCanvas,
                             Direction = Directions.Vertical,
                             ConnectionType = OutputLines[0].Output.ValueType,
                             ConnectionHash = OutputLines[0].Connections.Count > 0 //OutputLines[0].Output.IsConnected
                                                  ? OutputLines[0].Connections[0].ConnectionHash    // FIXME: Use all connections
                                                  : 0,
                             SlotId = OutputLines[0].Output.Id,
                         };

        foreach (var il in OutputLines)
        {
            yield return new AnchorPoint
                             {
                                 PositionOnCanvas = new Vector2(Width, (0.5f + il.VisibleIndex) * LineHeight) + PosOnCanvas,
                                 Direction = Directions.Horizontal,
                                 ConnectionType = il.Output.ValueType,
                                 ConnectionHash = il.Connections.Count > 0 //il.Output.IsConnected
                                                      ? il.Connections[0].ConnectionHash // FIXME: Use all connections
                                                      : 0,
                                 SlotId = il.Output.Id,
                             };
        }
    }

    public void ForOutputAnchors(Action<AnchorPoint> call)
    {
        if (OutputLines.Length == 0)
            return;

        call(new AnchorPoint
                 {
                     PositionOnCanvas = new Vector2(WidthHalf, Size.Y) + PosOnCanvas,
                     Direction = Directions.Vertical,
                     ConnectionType = OutputLines[0].Output.ValueType,
                     ConnectionHash = OutputLines[0].Output.HasInputConnections
                                          ? OutputLines[0].Output.GetConnection(0).GetHashCode()
                                          : 0,
                     SlotId = OutputLines[0].Output.Id,
                 });

        foreach (var il in OutputLines)
        {
            call(new AnchorPoint
                     {
                         PositionOnCanvas = new Vector2(Width, (0.5f + il.VisibleIndex) * LineHeight) + PosOnCanvas,
                         Direction = Directions.Horizontal,
                         ConnectionType = il.Output.ValueType,
                         ConnectionHash = il.Output.HasInputConnections
                                              ? OutputLines[0].Output.GetConnection(0).GetHashCode()
                                              : 0,
                         SlotId = il.Output.Id,
                     });
        }
    }
}