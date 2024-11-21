using T3.Core.Operator;
using T3.Core.Operator.Slots;

namespace T3.Editor.Gui.MagGraph.Ui;

internal sealed class MagGraphConnection
{
    public ConnectionStyles Style;
    public Vector2 SourcePos;
    public Vector2 TargetPos;
    
    public MagGraphItem SourceItem;
    public MagGraphItem TargetItem;
    public ISlot SourceOutput;
    public ISlot TargetInput => TargetItem.InputLines[InputLineIndex].Input;

    public Type Type => SourceOutput?.ValueType;
    //public IInputSlot TargetInput;
    public int InputLineIndex;
    public int OutputLineIndex;
    public int VisibleOutputIndex; // Do we need that?
    public int ConnectionHash;
    public int MultiInputIndex;

    public bool IsSnapped => Style < ConnectionStyles.BottomToTop;

    public enum ConnectionStyles
    {
        MainOutToMainInSnappedHorizontal = 0,
        MainOutToMainInSnappedVertical,
        MainOutToInputSnappedHorizontal,
        AdditionalOutToMainInputSnappedVertical,

        BottomToTop = 4,
        BottomToLeft,
        RightToTop,
        RightToLeft,
        
        Unknown,
    }

    public Symbol.Connection AsSymbolConnection()
    {
        return new Symbol.Connection(
                              SourceItem.Id,
                              SourceOutput.Id,
                              TargetItem.Id,
                              TargetInput.Id
                             );
    }

    public bool IsUnlinked;
}