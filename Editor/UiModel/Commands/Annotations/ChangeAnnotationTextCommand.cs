namespace T3.Editor.UiModel.Commands.Annotations;

public class ChangeAnnotationTextCommand : ICommand
{
    public string Name => "Change Annotation text";
    public bool IsUndoable => true;

    public ChangeAnnotationTextCommand(Annotation annotation, string newText)
    {
        _annotation = annotation;
        _originalText = annotation.Title;
        NewText = newText;
    }


    public void Do()
    {
        _annotation.Title = NewText;
    }
        
    public void Undo()
    {
        _annotation.Title = _originalText;
    }
        
        
    private readonly Annotation _annotation;
    private readonly string _originalText;
    public  string NewText { get; set; }
}