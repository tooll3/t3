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

internal sealed class SnapGraphItem : ISelectableCanvasObject
{
    public enum Categories
    {
        Operator,
        Input,
        Output,
    }

    public Guid Id { get; init; }
    public Categories Category;
    public Type PrimaryType = typeof(float);
    public ISelectableCanvasObject Selectable;
    public Vector2 PosOnCanvas { get => Selectable.PosOnCanvas; set => Selectable.PosOnCanvas = value; }
    public Vector2 Size { get; set; }
    //public bool IsSelected => NodeSelection.IsNodeSelected(this);
    public SnapGroup SnapGroup;
    public float UnitHeight => InputLines.Length + OutputLines.Length + 1;

    public override string ToString()
    {
        return ReadableName;
    }

    public SymbolUi SymbolUi;
    public Symbol.Child SymbolChild;
    public SymbolUi.Child SymbolChildUi;
    public Instance Instance;

    public string ReadableName
    {
        get
        {
            return Category switch
                       {
                           Categories.Operator => SymbolChild != null ? SymbolChild.ReadableName : "???",
                           Categories.Input    => Selectable is IInputUi inputUi ? inputUi.InputDefinition.Name : "???",
                           Categories.Output   => Selectable is IOutputUi outputUi ? outputUi.OutputDefinition.Name : "???",
                           _                   => "???"
                       };
        }
    }

    public InputLine[] InputLines;
    public OutputLine[] OutputLines;

    public struct InputLine
    {
        public Type Type;
        public Guid Id;
        //public SnapGraphItem TargetItem;
        public ISlot Input;
        public IInputUi InputUi;
        //public bool IsPrimary;
        public int VisibleIndex;
        public SnapGraphConnection ConnectionIn;
        public int MultiInputIndex;
    }

    public struct OutputLine
    {
        //public SnapGraphItem SourceItem;
        public ISlot Output;
        public IOutputUi OutputUi;
        //public bool IsPrimary;
        public int VisibleIndex;
        public int OutputIndex;
        public List<SnapGraphConnection> ConnectionsOut;
    }

    public struct AnchorPoint
    {
        public Vector2 PositionOnCanvas;
        public Directions Direction;
        public Type ConnectionType;
        public int ConnectionHash;
        public Guid SlotId;
    }

    public enum Directions
    {
        Horizontal,
        Vertical,
    }

    public const float Width = 140;
    public const float WidthHalf = Width / 2;
    public const float LineHeight = 35;
    public const float LineHeightHalf = LineHeight / 2;
    public static readonly Vector2 GridSize = new Vector2(Width, LineHeight);

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
    /// input anchor taken if
    /// - connected
    ///
    /// output anchor is taken if...
    /// - 
    /// </summary>
    public IEnumerable<AnchorPoint> GetOutputAnchors()
    {
        if (OutputLines.Length == 0)
            yield break;

        // vertical output...
        {
            yield return new AnchorPoint
                             {
                                 PositionOnCanvas = new Vector2(WidthHalf, Size.Y) + PosOnCanvas,
                                 Direction = Directions.Vertical,
                                 ConnectionType =  OutputLines[0].Output.ValueType,
                                 ConnectionHash = GetSnappedConnectionHash(OutputLines[0].ConnectionsOut),
                                 SlotId = OutputLines[0].Output.Id,
                             };
        }

        // Horizontal outputs
        {
            foreach (var outputLine in OutputLines)
            {
                yield return new AnchorPoint
                                 {
                                     PositionOnCanvas = new Vector2(Width, (0.5f + outputLine.VisibleIndex) * LineHeight) + PosOnCanvas,
                                     Direction = Directions.Horizontal,
                                     ConnectionType = outputLine.Output.ValueType,
                                     ConnectionHash = GetSnappedConnectionHash(outputLine.ConnectionsOut),
                                     SlotId = outputLine.Output.Id,
                                 };
            }
        }
    }

    /// <summary>
    /// Get input anchors with current position and orientation.
    /// </summary>
    /// <remarks>
    /// Using an Enumerable interface here is bad, because it creates a lot of allocations.
    /// In the long term, this should be cached.
    /// </remarks>
    // TODO: Inline to draw anchors
    public IEnumerable<AnchorPoint> GetInputAnchors()
    {
        if (InputLines.Length == 0)
            yield break;

        // Top input
        yield return new AnchorPoint
                         {
                             PositionOnCanvas = new Vector2(WidthHalf, 0) + PosOnCanvas,
                             Direction = Directions.Vertical,
                             ConnectionType = InputLines[0].Type,
                             ConnectionHash = InputLines[0].ConnectionIn?.ConnectionHash ?? FreeAnchor,
                             SlotId = InputLines[0].Id,
                         };
        // Side inputs
        foreach (var il in InputLines)
        {
            yield return new AnchorPoint
                             {
                                 PositionOnCanvas = new Vector2(0, (0.5f + il.VisibleIndex) * LineHeight) + PosOnCanvas,
                                 Direction = Directions.Horizontal,
                                 ConnectionType = il.Type,
                                 ConnectionHash = il.ConnectionIn?.ConnectionHash ?? FreeAnchor,
                                 SlotId = il.Id,
                             };
        }
    }

    /** Assume as free (I.e. not connected) unless on connection is snapped, then return this connection has hash. */
    private static int GetSnappedConnectionHash(List<SnapGraphConnection> snapGraphConnections)
    {
        foreach (var sc in snapGraphConnections)
        {
            if (!sc.IsSnapped)
                continue;

            return sc.ConnectionHash;
        }

        return FreeAnchor;
    }

    private const int FreeAnchor = 0;

    //
    // public void ForOutputAnchors(Action<AnchorPoint> call)
    // {
    //     if (OutputLines.Length == 0)
    //         return;
    //
    //     call(new AnchorPoint
    //              {
    //                  PositionOnCanvas = new Vector2(WidthHalf, Size.Y) + PosOnCanvas,
    //                  Direction = Directions.Vertical,
    //                  ConnectionType = OutputLines[0].Output.ValueType,
    //                  ConnectionHash = OutputLines[0].Output.HasInputConnections
    //                                       ? OutputLines[0].Output.GetConnection(0).GetHashCode()
    //                                       : 0,
    //                  SlotId = OutputLines[0].Output.Id,
    //              });
    //
    //     foreach (var il in OutputLines)
    //     {
    //         call(new AnchorPoint
    //                  {
    //                      PositionOnCanvas = new Vector2(Width, (0.5f + il.VisibleIndex) * LineHeight) + PosOnCanvas,
    //                      Direction = Directions.Horizontal,
    //                      ConnectionType = il.Output.ValueType,
    //                      ConnectionHash = il.Output.HasInputConnections
    //                                           ? OutputLines[0].Output.GetConnection(0).GetHashCode()
    //                                           : 0,
    //                      SlotId = il.Output.Id,
    //                  });
    //     }
    // }

    public void Select(NodeSelection nodeSelection)
    {
        if (Category == SnapGraphItem.Categories.Operator)
        {
            //nodeSelection.SetSelectionToChildUi(this, Instance);
            nodeSelection.SetSelection(this, Instance);
        }
        else
        {
            nodeSelection.SetSelection(this);
        }
    }

    public void AddToSelection()
    {
    }
}