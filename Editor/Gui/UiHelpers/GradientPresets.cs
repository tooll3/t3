using System.IO;
using T3.Core.DataTypes;
using T3.Core.UserData;
using T3.Serialization;

namespace T3.Editor.Gui.UiHelpers;

public static class GradientPresets
{
    public static List<Gradient> Presets
    {
        get
        {
            if(_presets != null)
                return _presets;

            var loaded = UserData.TryLoadOrWriteToUser(FileName, out var text);

            if (!loaded)
                _presets = [];
            else
            {
                _presets = JsonUtils.TryLoadingJson<List<Gradient>>(text) ?? [];
            }

            return _presets;
        }
    }

    public static void Save()
    {
        JsonUtils.TrySaveJson(_presets, FilePath);    
    }
    
    private static List<Gradient> _presets;

    private static string FilePath => Path.Combine(UserData.SettingsFolder, FileName);
    const string FileName = "gradients.json";
}