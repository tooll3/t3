using System;
using System.Collections.Generic;
using System.Linq;

namespace T3.Core.Commands
{
    public interface ICommand
    {
        string Name { get; }
        bool IsUndoable { get; }
        void Undo();
        void Do();
    }

    public class MacroCommand : ICommand
    {
        public MacroCommand(string name, IEnumerable<ICommand> commands)
        {
            _name = name;
            _commands = commands.ToList();
        }

        public string Name => _name;

        public bool IsUndoable => _commands.Aggregate(true, (result, current) => result && current.IsUndoable);

        public void Do()
        {
            _commands.ForEach(c => c.Do());
        }

        public void Undo()
        {
            var tmpCmds = new List<ICommand>(_commands);
            tmpCmds.Reverse();
            tmpCmds.ForEach(c => c.Undo());
        }

        protected string _name = string.Empty;
        protected List<ICommand> _commands;
    }

    public class UndoRedoStack : IDisposable
    {
        public void Dispose()
        {
        }

        public void AddAndExecute(ICommand command)
        {
            Add(command);

            command.Do();
        }

        public void Add(ICommand command)
        {
            if (command.IsUndoable)
            {
                _undoStack.Push(command);
                _redoStack.Clear();
            }
            else
            {
                Clear();
            }
        }

        public void Undo()
        {
            if (_undoStack.Count > 0)
            {
                var command = _undoStack.Pop();
                command.Undo();
                _redoStack.Push(command);
            }
        }

        public void Redo()
        {
            if (_redoStack.Count > 0)
            {
                var command = _redoStack.Pop();
                command.Do();
                _undoStack.Push(command);
            }
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        private readonly Stack<ICommand> _undoStack = new Stack<ICommand>();
        private readonly Stack<ICommand> _redoStack = new Stack<ICommand>();
    }
}