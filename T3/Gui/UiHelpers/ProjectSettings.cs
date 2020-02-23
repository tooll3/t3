using System;

namespace T3.Gui.UiHelpers
{
    /// <summary>
    /// Saves view layout and currently open node 
    /// </summary>
    public  class ProjectSettings :Settings
    {
        public ProjectSettings()
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            Config = TryLoading<ConfigData>("projectSettings.json")?? new ConfigData();
        }
        
        public static  ConfigData Config;
        
        public class ConfigData
        {
            public string SoundtrackFilepath = "";
            public float SoundtrackBpm=120;
        }
        
        
        private  void OnProcessExit(object sender, EventArgs e)
        {
            SaveSettings(Config);
        }
    }
}