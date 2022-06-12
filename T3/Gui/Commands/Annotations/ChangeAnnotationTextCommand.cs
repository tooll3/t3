using System;
using System.Diagnostics;
using System.Linq;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Graph;
using T3.Gui.Graph.Interaction;

namespace T3.Gui.Commands
{
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
}