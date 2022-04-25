using T3.Gui;
using T3.Gui.Commands;
using t3.Gui.Graph;

namespace t3.Gui.Commands.Annotations
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