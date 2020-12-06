using System;
using System.Collections.Generic;

namespace T3.Gui.Interaction.PresetSystem
{
    public class PresetScene
    {
    }

    public class ParameterGroup
    {
        public Guid Id = Guid.NewGuid();
        public string Title;
        public List<GroupParameter> Parameters = new List<GroupParameter>(16);

        
        public void AddParameterToIndex(GroupParameter parameter, int index)
        {
            // Extend list
            while (Parameters.Count <= index)
            {
                Parameters.Add(null);
            }

            Parameters[index] = parameter;
        }
    }

    public class GroupParameter
    {
        public Guid Id;
        public string Title; 
        public Guid SymbolChildId;
        public Guid InputId;
        public int ComponentIndex;    // for handling Vector inputs
        public Type InputType;
    }

    public class Preset
    {
        public bool IsPlaceholder;
    }
}