using T3.Core.Animation;
using T3.Core.Operator;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Windows.TimeLine
{
    /// <summary>
    /// Interface common to Timeline components that can hold a selection and manipulate
    /// a selection (like <see cref="Animator.Clip"/>, keyframes, etc).
    /// </summary>
    public interface ITimeObjectManipulation
    {
        void ClearSelection();
        void UpdateSelectionForArea(ImRect area, SelectionFence.SelectModes selectMode);
        void DeleteSelectedElements(Instance compositionOp);

        ICommand StartDragCommand(in Guid symbolId);
        void UpdateDragCommand(double dt, double dv);
        void UpdateDragStretchCommand(double scaleU, double scaleV, double originU, double originV);
        void CompleteDragCommand();
        
        void UpdateDragAtStartPointCommand(double dt, double dv);
        void UpdateDragAtEndPointCommand(double dt, double dv);

        TimeRange GetSelectionTimeRange();
    }
}