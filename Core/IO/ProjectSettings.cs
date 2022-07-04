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
            public string MainOperatorName = "";
            public float AudioResyncThreshold = 1.65f / 60f;
            
            public string AudioInputDeviceName = string.Empty;
            public float AudioGainFactor = 1;
            public float AudioDecayFactor = 0.98f;
        }
    }
}