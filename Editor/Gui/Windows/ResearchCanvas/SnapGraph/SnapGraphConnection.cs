using System.Numerics;
using T3.Core.Operator.Slots;

namespace T3.Editor.Gui.Windows.ResearchCanvas.SnapGraph;

public class SnapGraphConnection
{
    public ConnectionStyles Style;
    public Vector2 SourcePos;
    public Vector2 TargetPos;
    public SnapGraphItem SourceItem;
    public SnapGraphItem TargetItem;
    public ISlot SourceOutput;
    public IInputSlot TargetInput;

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
}