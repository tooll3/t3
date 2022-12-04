using System.Collections.Generic;
using System.Linq;

namespace T3.Editor.Gui.Commands
{
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
            var tmpCommands = new List<ICommand>(_commands);
            tmpCommands.Reverse();
            tmpCommands.ForEach(c => c.Undo());
        }

        protected readonly string _name = string.Empty;
        protected readonly List<ICommand> _commands;
    }
}