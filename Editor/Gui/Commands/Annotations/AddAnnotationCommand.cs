using System.Collections.Generic;
using T3.Editor.Gui.Graph;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Commands.Annotations
{
    public class AddAnnotationCommand : ICommand
    {
        public string Name => "Add Preset";
        public bool IsUndoable => true;
        
        private SymbolUi _symbolUi;
        private Annotation _newAnnotation;
        
        private readonly Dictionary<Annotation, Annotation> _originalDefForReferences = new();
        private readonly Dictionary<Annotation, Annotation> _newDefForReferences = new();

        public AddAnnotationCommand(SymbolUi symbolUi, Annotation annotation)
        {
            _symbolUi = symbolUi;
            _newAnnotation = annotation;
        }
        
        public void Do()
        {
            _symbolUi.Annotations[_newAnnotation.Id] = _newAnnotation;
            _symbolUi.FlagAsModified();
        }
        
        public void Undo()
        {
            _symbolUi.Annotations.Remove(_newAnnotation.Id);
            _symbolUi.FlagAsModified();
        }
    }
}