namespace T3.Gui.Interaction.PresetSystem.Model
{
    public struct PresetAddress
    {
        public PresetAddress(int groupIndex, int sceneIndex, bool isValid = true)
        {
            GroupColumn = groupIndex;
            SceneRow = sceneIndex;
            IsValid = isValid;
        }

        public readonly int GroupColumn;
        public readonly int SceneRow;
        public readonly bool IsValid;

        public bool IsValidForContext(PresetContext config)
        {
            return GroupColumn >= 0
                   && SceneRow >= 0
                   && GroupColumn < config.Presets.GetLength(0)
                   && SceneRow < config.Presets.GetLength(1);
        }

        public static PresetAddress operator +(PresetAddress a, PresetAddress b)
        {
            return new PresetAddress(a.GroupColumn + b.GroupColumn,
                                     a.SceneRow + b.SceneRow);
        }

        public static PresetAddress operator -(PresetAddress a, PresetAddress b)
        {
            return new PresetAddress(a.GroupColumn - b.GroupColumn,
                                     a.SceneRow - b.SceneRow);
        }

        public static PresetAddress NotAnAddress = new PresetAddress(-1, -1, false);
        public override string ToString()
        {
            return IsValid ? $"{GroupColumn}:{SceneRow}" : "NaA";
        }
    }
}