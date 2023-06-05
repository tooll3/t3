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
            public bool TimeClipSuspending = false;
            public string MainOperatorName = "";
            public float AudioResyncThreshold = 0.04f;
            public bool EnablePlaybackControlWithKeyboard = true;
        }
    }
}