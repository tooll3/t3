using System;
using System.Collections.Generic;
using System.IO;
using T3.Core.Logging;
using T3.Gui.Graph;

namespace T3.Gui.UiHelpers
{
    /// <summary>
    /// Saves view layout and currently open node 
    /// </summary>
    public class UserSettings
    {
        public UserSettings()
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            TryLoadingSettings();
        }

        public static Dictionary<Guid, ScalableCanvas.CanvasProperties> CanvasPropertiesForSymbols = new Dictionary<Guid, ScalableCanvas.CanvasProperties>();

        void OnProcessExit(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private static void SaveSettings()
        {
            var serializer = Newtonsoft.Json.JsonSerializer.Create();
            var writer = new StringWriter();
            serializer.Serialize(writer, CanvasPropertiesForSymbols);

            var file = File.CreateText(UserSettingFilepath);
            file.Write(writer.ToString());
            file.Close();
        }

        private static void TryLoadingSettings()
        {
            if (!File.Exists(UserSettingFilepath))
            {
                Log.Warning($"Layout {UserSettingFilepath} doesn't exist yet");
                return;
            }

            var jsonBlob = File.ReadAllText(UserSettingFilepath);
            var serializer = Newtonsoft.Json.JsonSerializer.Create();
            var fileTextReader = new StringReader(jsonBlob);
            if (!(serializer.Deserialize(fileTextReader, typeof(Dictionary<Guid, ScalableCanvas.CanvasProperties>))
                      is Dictionary<Guid, ScalableCanvas.CanvasProperties> configurations))
            {
                Log.Error("Can't load layout");
                return;
            }

            CanvasPropertiesForSymbols = configurations;
        }

        private const string UserSettingFilepath = "userSettings.json";
    }
}