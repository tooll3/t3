using System.Numerics;
using T3.Gui.Selection;

namespace T3.Gui.Commands
{
    public class ChangeSelectableCommand : ICommand
    {
        public string Name => "Add Symbol Child";
        public bool IsUndoable => true;

        public ChangeSelectableCommand(ISelectable selectable)
        {
            OriginalPosOnCanvas = selectable.PosOnCanvas;
            OriginalSize = selectable.Size;
            OriginalIsSelected = selectable.IsSelected;

            PosOnCanvas = OriginalPosOnCanvas;
            Size = OriginalSize;
            IsSelected = OriginalIsSelected;

            _selectable = selectable;
        }

        public void Undo()
        {
            _selectable.PosOnCanvas = OriginalPosOnCanvas;
            _selectable.Size = OriginalSize;
            _selectable.IsSelected = IsSelected;
        }

        public void Do()
        {
            _selectable.PosOnCanvas = PosOnCanvas;
            _selectable.Size = Size;
            _selectable.IsSelected = IsSelected;
        }

        public Vector2 OriginalPosOnCanvas { get; set; }
        public Vector2 OriginalSize { get; set; }
        public bool OriginalIsSelected { get; set; }

        public Vector2 PosOnCanvas { get; set; }
        public Vector2 Size { get; set; }
        public bool IsSelected { get; set; }

        private readonly ISelectable _selectable;
    }
}
