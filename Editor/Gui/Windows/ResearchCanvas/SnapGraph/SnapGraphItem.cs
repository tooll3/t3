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

namespace T3.Editor.Gui.Windows.ResearchCanvas.SnapGraph;

public class SnapGraphItem : ISelectableCanvasObject
{
    public Guid Id { get; init; }
    public readonly Type PrimaryType = typeof(float);
    public Vector2 PosOnCanvas { get => SymbolChildUi.PosOnCanvas; set => SymbolChildUi.PosOnCanvas = value; }
    public Vector2 Size { get => SymbolChildUi.Size; set => SymbolChildUi.Size = value; }
    public bool IsSelected => NodeSelection.IsNodeSelected(SymbolChildUi);
    public SnapGroup SnapGroup;
    public float UnitHeight => InputLines.Count + OutputLines.Count + 1;

    public override string ToString()
    {
        return SymbolChild.ReadableName;
    }

    public SymbolUi SymbolUi;
    public SymbolChild SymbolChild;
    public SymbolChildUi SymbolChildUi;
    public Instance Instance;

    public readonly List<InputLine> InputLines = new(4);
    public readonly List<OutputLine> OutputLines = new(1);

    public struct InputLine
    {
        public IInputSlot Input;
        public IInputUi InputUi;
        public bool IsPrimary;
        public int VisibleIndex;
    }

    public struct OutputLine
    {
        public ISlot Output;
        public IOutputUi OutputUi;
        public bool IsPrimary;
        public int VisibleIndex;
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
            if (other.ConnectionType != ConnectionType ||
                other.Direction != Direction ||
                other.ConnectionHash != ConnectionHash)
                return float.PositiveInfinity;

            return Vector2.Distance(other.PositionOnCanvas, PositionOnCanvas);
        }
    }

    public readonly List<AnchorPoint> InputAnchors = new(4);
    public readonly List<AnchorPoint> OutputAnchors = new(4);

    public enum Directions
    {
        Horizontal,
        Vertical,
    }

    public static readonly Vector2 GridSize = new Vector2(80, 20);
    public bool IsDragged; // FIXME: Implement

    public ImRect Area => ImRect.RectWithSize(PosOnCanvas, Size);

    public static ImRect GetGroupBoundingBox(List<SnapGraphItem> items)
    {
        ImRect extend = default;
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            if (index == 0)
            {
                extend = item.Area;
            }
            else
            {
                extend.Add(item.Area);                
            }
        }
        return extend;
    }
    
}