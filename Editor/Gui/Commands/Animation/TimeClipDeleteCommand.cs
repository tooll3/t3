using System.Collections.Generic;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;

namespace T3.Editor.Gui.Commands.Animation;

public class TimeClipDeleteCommand : ICommand
{
    public string Name => "Delete time clip";
    public bool IsUndoable => true;

    public TimeClipDeleteCommand(Instance compositionOp, IEnumerable<ITimeClip> clipsToRemove)
    {
        _clipsToRemove = clipsToRemove;
        _compositionOp = compositionOp;
    }

    public void Do()
    {
        Log.Warning("Deleting timeclips is not implemented");
    }

    public void Undo()
    {
        Log.Warning("Undoing deletion of timeclips is not implemented");
    }

    private readonly Instance _compositionOp;
    private readonly IEnumerable<ITimeClip> _clipsToRemove;
}