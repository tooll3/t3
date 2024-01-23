using System;

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
            public bool TimeClipSuspending = true;
            public Guid MainOperatorGuid = Guid.Empty;
            public float AudioResyncThreshold = 0.04f;
            public bool EnablePlaybackControlWithKeyboard = true;
            public bool WindowedMode = false;
            
            public string LimitMidiDeviceCapture = null; 
            public bool EnableMidiSnapshotIndication = false;
        }
    }
}