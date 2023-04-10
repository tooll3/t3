using System.Collections.Generic;
using System.Linq;
using T3.Core.Logging;

namespace T3.Editor.Gui.Commands
{
    public class MacroCommand : ICommand
    {
        public MacroCommand(string name, IEnumerable<ICommand> commands)
        {
            Name = name;
            _commands = commands.ToList();
        }
        
        public MacroCommand(string name)
        {
            Name = name;
            _commands = new List<ICommand>();
        }

        public string Name { get; set; }
        private bool _lastCommandAlreadyExecuted;
        
        public bool IsUndoable => _commands.Aggregate(true, (result, current) => result && current.IsUndoable);

        /// <summary>
        /// All commands must be added before executing the command.
        /// </summary>
        public void AddCommand(ICommand command)
        {
            _commands.Add(command);
        }
        
        /// <summary>
        /// For certain macro-operations it can be necessary to executed some of its sub commands
        /// early on. If this is the case ALL further commands need to be added end executed immediately.
        /// </summary>
        public void AddAndExecCommand(ICommand command)
        {
            if (_commands.Count > 0 && !_lastCommandAlreadyExecuted)
            {
                Log.Warning($"Can't have {_commands.Count} non-executed macro-commands before AddAndExecuted.");
            }

            _lastCommandAlreadyExecuted = true;
            _commands.Add(command);
            command.Do();
        }
        
        public void Do()
        {
            _commands.ForEach(c => c.Do());
        }

        public void Undo()
        {
            var tmpCommands = new List<ICommand>(_commands);
            tmpCommands.Reverse();
            tmpCommands.ForEach(c => c.Undo());
        }

        public int Count => _commands.Count;

        // protected string _name = string.Empty;
        protected readonly List<ICommand> _commands;
    }
}