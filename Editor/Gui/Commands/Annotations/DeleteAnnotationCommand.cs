using T3.Editor.Gui.Graph;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Commands.Annotations
{
    public class DeleteAnnotationCommand : ICommand
    {
        public string Name => "Delete Annotation";
        public bool IsUndoable => true;
        
        private readonly SymbolUi _symbolUi;
        private readonly Annotation _originalAnnotation;
        
        public DeleteAnnotationCommand(SymbolUi symbolUi, Annotation annotation)
        {
            _symbolUi = symbolUi;
            _originalAnnotation = annotation;
        }
        
        public void Undo()
        {
            _symbolUi.Annotations[_originalAnnotation.Id] = _originalAnnotation;
        }

        public void Do()
        {
            _symbolUi.Annotations.Remove(_originalAnnotation.Id);
        }
    }
}