using System;

namespace T3.Gui.UiHelpers
{
    /// <summary>
    /// Saves view layout and currently open node 
    /// </summary>
    public class ProjectSettings : Settings<ProjectSettings.ConfigData>
    {
        public ProjectSettings() : base("projectSettings.json")
        {
        }

        public class ConfigData
        {
            public string SoundtrackFilepath = "";
            public bool UseBpmRate = true;
            public float SoundtrackBpm = 120;
        }
    }
}