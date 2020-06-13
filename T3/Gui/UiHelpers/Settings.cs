using System;
using System.IO;
using Newtonsoft.Json;
using T3.Core.Logging;

namespace T3.Gui.UiHelpers
{
    /// <summary>
    /// Implements writing and reading configuration files 
    /// </summary>
    public abstract class Settings
    {
        private string _filepath;

        protected T TryLoading<T>(string filepath) where T : class
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

        protected void SaveSettings<T>(T configuration)
        {
            Log.Debug($"Saving {_filepath}...");
            var serializer = JsonSerializer.Create();
            serializer.Formatting = Formatting.Indented;
            using (var file = File.CreateText(_filepath))
            {
                serializer.Serialize(file, configuration);
            }
        }
    }
}