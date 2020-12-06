namespace T3.Gui.Interaction.PresetSystem.Model
{
    public struct PresetAddress
    {
        public PresetAddress(int groupIndex, int sceneIndex, bool isValid = true)
        {
            ParameterGroupColumn = groupIndex;
            SceneRow = sceneIndex;
            IsValid = isValid;
        }

        public int ParameterGroupColumn;
        public int SceneRow;
        public bool IsValid;

        public bool IsValidForContext(PresetContext config)
        {
            return ParameterGroupColumn >= 0
                   && SceneRow >= 0
                   && ParameterGroupColumn < config.Presets.GetLength(0)
                   && SceneRow < config.Presets.GetLength(1);
        }

        public static PresetAddress NotAnAddress = new PresetAddress(-1, -1, false);
    }
}