using System.Collections.Generic;
using System.Linq;

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

        public bool IsUndoable => _commands.Aggregate(true, (result, current) => result && current.IsUndoable);

        /// <summary>
        /// All commands must be added before executing the command.
        /// </summary>
        public void AddCommand(ICommand command)
        {
            _commands.Add(command);
        }
        
        /// <summary>
        /// All commands must be added before executing the command.
        /// </summary>
        public void AddCommands(IEnumerable<ICommand> commands)
        {
            _commands.AddRange(commands);
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