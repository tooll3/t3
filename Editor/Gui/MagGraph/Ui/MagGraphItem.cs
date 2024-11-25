#nullable enable
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.OutputUi;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

// ReSharper disable UseWithExpressionToCopyStruct

namespace T3.Editor.Gui.MagGraph.Ui;

internal sealed class MagGraphItem : ISelectableCanvasObject
{
    public enum Variants
    {
        Operator,
        Input,
        Output,
        Obsolete,
    }

    internal int LastUpdateCycle;
    public Guid Id { get; init; }
    public Variants Variant;
    public Type PrimaryType = typeof(float);
    public required ISelectableCanvasObject Selectable;
    public Vector2 PosOnCanvas { get => Selectable.PosOnCanvas; set => Selectable.PosOnCanvas = value; }
    public Vector2 Size { get; set; }
    
    public ImRect Area => ImRect.RectWithSize(PosOnCanvas, Size);


    //public bool IsSelected => NodeSelection.IsNodeSelected(this);
    public MagGroup? MagGroup;

    public override string ToString() => ReadableName;

    public SymbolUi? SymbolUi;
    public Symbol.Child? SymbolChild;
    public Instance Instance;

    public string ReadableName
    {
        get
        {
            return Variant switch
                       {
                           Variants.Operator => SymbolChild != null ? SymbolChild.ReadableName : "???",
                           Variants.Input    => Selectable is IInputUi inputUi ? inputUi.InputDefinition.Name : "???",
                           Variants.Output   => Selectable is IOutputUi outputUi ? outputUi.OutputDefinition.Name : "???",
                           _                 => "???"
                       };
        }
    }
    
    // public bool IsSelected(NodeSelection nodeSelection)
    // {
    //     return nodeSelection.Selection.Any(c => c.Id == Id);
    // }
    public void Reset(int updateCycle)
    {
        InputLines = Array.Empty<InputLine>();
        OutputLines = Array.Empty<OutputLine>();
        LastUpdateCycle = updateCycle;
    }

    public InputLine[] InputLines = Array.Empty<InputLine>();
    public OutputLine[] OutputLines = Array.Empty<OutputLine>();

    public struct InputLine
    {
        public Type Type;
        public Guid Id;
        public ISlot Input;
        public IInputUi InputUi;
        public int VisibleIndex;
        public MagGraphConnection? ConnectionIn;
        public int MultiInputIndex;
    }

    public struct OutputLine
    {
        public Guid Id;
        public ISlot Output;
        public IOutputUi? OutputUi;
        public int VisibleIndex;
        public int OutputIndex;
        public List<MagGraphConnection> ConnectionsOut;
    }

    public struct AnchorPoint
    {
        public Vector2 PositionOnCanvas;
        public Directions Direction;
        public Type ConnectionType;
        public int ConnectionHash;
        public Guid SlotId;
        
        /** Test if a could be split be inserting b */
        public bool CountBeSplitBy(AnchorPoint b)
        {
            return ConnectionType == b.ConnectionType
                   && Direction == b.Direction
                   && ConnectionHash != FreeAnchor
                   && b.ConnectionHash == FreeAnchor;
        }
    }

    public enum Directions
    {
        Horizontal,
        Vertical,
    }

    public const float Width = 140;
    public const float WidthHalf = Width / 2;
    public const float LineHeight = 35;
    public static readonly Vector2 GridSize = new(Width, LineHeight);


    public static ImRect GetItemsBounds(IEnumerable<MagGraphItem> items)
    {
        ImRect extend = default;
        var index = 0;

        // Can't use list because enumerable...
        foreach (var item in items)
        {
            if (index == 0)
            {
                extend = item.Area;
            }
            else
            {
                extend.Add(item.Area);
            }

            index++;
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
                                 ConnectionType = OutputLines[0].Output.ValueType,
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

    /** Assume as free (I.e. not connected) unless an connection is snapped, then return this connection as hash. */
    private static int GetSnappedConnectionHash(List<MagGraphConnection> snapGraphConnections)
    {
        foreach (var sc in snapGraphConnections)
        {
            if (!sc.IsSnapped)
                continue;

            return sc.ConnectionHash;
        }

        return FreeAnchor;
    }

    public const int FreeAnchor = 0;

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
        // TODO: Avoid this by not using magGraphItem as selectable
        if (Variant == MagGraphItem.Variants.Operator
            && Instance?.Parent != null
            && SymbolUiRegistry.TryGetSymbolUi(Instance.Parent.Symbol.Id, out var parentSymbolUi)
            && parentSymbolUi.ChildUis.TryGetValue(Instance.SymbolChildId, out var childUi))
        {
            nodeSelection.SetSelection(childUi, Instance);
        }
        else
        {
            nodeSelection.SetSelection(this);
        }
    }

    public void AddToSelection(NodeSelection nodeSelection)
    {
        // TODO: Avoid this by not using magGraphItem as selectable
        if (Variant == MagGraphItem.Variants.Operator
            && Instance?.Parent != null
            && SymbolUiRegistry.TryGetSymbolUi(Instance.Parent.Symbol.Id, out var parentSymbolUi)
            && parentSymbolUi.ChildUis.TryGetValue(Instance.SymbolChildId, out var childUi))
        {
            nodeSelection.AddSelection(childUi, Instance);
        }
        else
        {
            nodeSelection.AddSelection(this);
        }
    }
}