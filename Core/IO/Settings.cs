using System;
using System.IO;
using Newtonsoft.Json;
using T3.Core.Logging;

namespace T3.Core.IO
{
    /// <summary>
    /// Implements writing and reading configuration files 
    /// </summary>
    public class Settings<T> where T : class, new()
    {
        public static T Config;

        protected Settings(string filepath, bool saveOnQuit)
        {
            if(saveOnQuit)
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            
            Config = TryLoading(filepath) ?? new T();
            _instance = this;
        }
        

        private T TryLoading(string filepath)
        {
            _filepath = filepath;
            if (!File.Exists(_filepath))
            {
                Log.Warning($"{_filepath} doesn't exist yet");
                return null;
            }

            var jsonBlob = File.ReadAllText(_filepath);
            var serializer = JsonSerializer.Create();
            var fileTextReader = new StringReader(jsonBlob);
            try
            {
                if (serializer.Deserialize(fileTextReader, typeof(T)) is T configurations)
                    return configurations;
            }
            catch (Exception e)
            {
                Log.Error($"Can't load {_filepath}:" + e.Message);
                return null;
            }

            Log.Error($"Can't load {_filepath}");
            return null;
        }

        private void SaveSettings(T configuration)
        {
            Log.Debug($"Saving {_filepath}...");
            var serializer = JsonSerializer.Create();
            serializer.Formatting = Formatting.Indented;
            using (var file = File.CreateText(_filepath))
            {
                serializer.Serialize(file, configuration);
            }
        }

        private void OnProcessExit(object sender, EventArgs e)
        {
            SaveSettings(Config);
        }

        public static void Save()
        {
            _instance.SaveSettings(Config);
        }

        private static Settings<T> _instance;
        private string _filepath;
    }
}