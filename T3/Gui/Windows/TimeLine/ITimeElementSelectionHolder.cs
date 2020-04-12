using T3.Core.Animation;
using T3.Core.Operator;
using T3.Gui.Commands;
using UiHelpers;

namespace T3.Gui.Windows.TimeLine
{
    /// <summary>
    /// Interface common to Timeline components that can hold a selection and manipulate
    /// a selection (like <see cref="Animator.Clip"/>, keyframes, etc).
    /// </summary>
    public interface ITimeElementSelectionHolder
    {
        void ClearSelection();
        void UpdateSelectionForArea(ImRect area, SelectMode selectMode);
        void DeleteSelectedElements();

        ICommand StartDragCommand();
        void UpdateDragCommand(double dt, double dv);
        void UpdateDragStretchCommand(double scaleU, double scaleV, double originU, double originV);
        void CompleteDragCommand();
        
        void UpdateDragAtStartPointCommand(double dt, double dv);
        void UpdateDragAtEndPointCommand(double dt, double dv);

        TimeRange GetSelectionTimeRange();
    }

    
    public enum SelectMode
    {
        Add = 0,
        Remove,
        Replace
    }
}