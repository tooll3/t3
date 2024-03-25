namespace T3.Editor.Gui.Selection
{
    public interface ISelectableCanvasObject
    {
        Guid Id { get; }
        Vector2 PosOnCanvas { get; set; }
        Vector2 Size { get; set; }
        public Vector2 TopRightPosOnCanvas => PosOnCanvas + Size;
    }

    public interface ISelectionContainer
    {
        IEnumerable<ISelectableCanvasObject> GetSelectables();
    }

    public interface ISelection
    {
        public bool IsNodeSelected(ISelectableCanvasObject obj);
    }
}
