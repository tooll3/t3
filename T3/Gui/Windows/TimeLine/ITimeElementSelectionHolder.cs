using T3.Core.Operator;
using T3.Gui.Commands;
using UiHelpers;

namespace T3.Gui.Windows.TimeLine
{
    /// <summary>
    /// Interface common to Timeline components that can hold a selection and manipulate a selection (like <see cref="Animator.Clip"/>, keyframes, etc).
    /// </summary>
    interface ITimeElementSelectionHolder
    {
        void ClearSelection();
        void UpdateSelectionForArea(ImRect area, SelectMode selectMode);
        //Command DeleteSelectedElements();

        ICommand StartDragCommand();
        void UpdateDragCommand(double dt, double dv);
        void CompleteDragCommand();
        void UpdateDragStartCommand(double dt, double dv);
        void UpdateDragEndCommand(double dt, double dv);
    }
    
    public enum SelectMode
    {
        Add = 0,
        Remove,
        Replace
    }
}