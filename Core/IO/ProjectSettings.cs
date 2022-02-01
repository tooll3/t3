namespace T3.Core.IO
{
    /// <summary>
    /// Saves view layout and currently open node 
    /// </summary>
    public class ProjectSettings : Settings<ProjectSettings.ConfigData>
    {
        public ProjectSettings(bool saveOnQuit) : base("projectSettings.json", saveOnQuit)
        {
        }
        
        public class ConfigData
        {
            public string SoundtrackFilepath = "";
            public bool UseBpmRate = true;
            public float SoundtrackBpm = 120;
            public double SoundtrackOffset = 0;
            public string MainOperatorName = "";
            public double SlideHack = 0;
        }
    }
}