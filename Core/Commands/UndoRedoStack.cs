using System;
using System.Collections.Generic;
using System.Dynamic;
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

    public static class UndoRedoStack
    {
        public static bool CanUndo => _undoStack.Count > 0;
        public static bool CanRedo => _redoStack.Count > 0;
        public static ICommand CommandInFlight { get; set; }

        public static void AddAndExecute(ICommand command)
        {
            Add(command);

            command.Do();
        }

        public static void Add(ICommand command)
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

        public static void AddCommandInFlightToStack()
        {
            Add(CommandInFlight);
            CommandInFlight = null;
        }

        public static void Undo()
        {
            if (CanUndo)
            {
                var command = _undoStack.Pop();
                command.Undo();
                _redoStack.Push(command);
            }
        }

        public static void Redo()
        {
            if (CanRedo)
            {
                var command = _redoStack.Pop();
                command.Do();
                _undoStack.Push(command);
            }
        }

        public static void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        private static readonly Stack<ICommand> _undoStack = new Stack<ICommand>();
        private static readonly Stack<ICommand> _redoStack = new Stack<ICommand>();
    }
}