using System;

namespace T3.Gui.Interaction.PresetSystem.Model
{
    public class GroupParameter
    {
        public Guid Id;
        public string Title; 
        public Guid SymbolChildId;
        public Guid InputId;
        public int ComponentIndex;    // for handling Vector inputs
        public Type InputType;
    }
}