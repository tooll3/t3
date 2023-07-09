using System;
using T3.Serialization;

namespace T3.Core.IO
{
    /// <summary>
    /// Implements writing and reading configuration files 
    /// </summary>
    public class Settings<T> where T : class, new()
    {
        public static T Config;
        public static T Defaults;

        protected Settings(string filepath, bool saveOnQuit)
        {
            if(saveOnQuit)
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            Defaults = new T();
            Config = JsonUtils.TryLoadingJson<T>(filepath) ?? new T();
            _filepath = filepath;
            _instance = this;
        }

        private static void OnProcessExit(object sender, EventArgs e)
        {
            Save();
        }

        public static void Save()
        {
            JsonUtils.SaveJson(Config, _instance._filepath);
        }

        private static Settings<T> _instance;
        private readonly string _filepath;
    }
}