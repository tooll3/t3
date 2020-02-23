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
        private  string _filepath;

        protected  T TryLoading<T>(string filepath) where T:class
        {
            _filepath = filepath;
            if (!File.Exists(_filepath))
            {
                Log.Warning($"Layout {_filepath} doesn't exist yet");
                return null;
            }

            var jsonBlob = File.ReadAllText(_filepath);
            var serializer = Newtonsoft.Json.JsonSerializer.Create();
            var fileTextReader = new StringReader(jsonBlob);
            if (serializer.Deserialize(fileTextReader, typeof(T))
                    is T configurations)
                return configurations;
            
            Log.Error("Can't load layout");
            return null;
        }

        
        protected  void SaveSettings<T>(T configuration)
        {
            Log.Debug("Saving user settings...");
            var serializer = JsonSerializer.Create();
            serializer.Formatting = Formatting.Indented;
            using (var file = File.CreateText(_filepath))
            {
                serializer.Serialize(file, configuration);
            }
        }
    }
}