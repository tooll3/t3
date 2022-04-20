using System.Collections.Generic;
using t3.Gui.Graph;

namespace T3.Gui.Commands
{
    public class AddAnnotationCommand : ICommand
    {
        public string Name => "Add Preset";
        public bool IsUndoable => true;
        
        private SymbolUi _symbolUi;
        private Annotation _newAnnotation;
        
        private readonly Dictionary<Annotation, Annotation> _originalDefForReferences = new Dictionary<Annotation, Annotation>();
        private readonly Dictionary<Annotation, Annotation> _newDefForReferences = new Dictionary<Annotation, Annotation>();

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