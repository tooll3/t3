using System.Collections.Generic;
using T3.Core.DataTypes;
using T3.Serialization;

namespace T3.Editor.Gui.UiHelpers;

public static class GradientPresets
{
    public static List<Gradient> Presets => _presets
                                                ??= JsonUtils.TryLoadingJson<List<Gradient>>(FilePath)
                                                    ?? new List<Gradient>();

    public static void Save()
    {
        JsonUtils.SaveJson(_presets, FilePath);    
    }
    
    private static List<Gradient> _presets;

    private const string FilePath = ".t3/gradients.json";
}