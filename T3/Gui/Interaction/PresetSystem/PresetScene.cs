using System;
using System.Collections.Generic;

namespace T3.Gui.Interaction.PresetControl
{
    /// <summary>
    /// Model of a single composition
    /// </summary>
    public class PresetConfiguration
    {
        public List<PresetScene> Scenes = new List<PresetScene>();
        public List<ParameterGroup> ParameterGroups = new List<ParameterGroup>();
        public Preset[,] Presets = new Preset[4, 4];
        public PresetAddress ViewWindow;

        public Preset TryGetPreset(PresetAddress address)
        {
            if (!address.IsValidForConfig(this))
                return null;

            var p = Presets[address.ParameterGroupColumn, address.SceneRow];
            return p ?? _mockPreset;
        }

        
        private static readonly Preset _mockPreset = new Preset() { IsPlaceholder = true};
        
        public void SetPresetAt(Preset preset, PresetAddress address)
        {
            var needToExtendGrid = !address.IsValidForConfig(this);
            if (needToExtendGrid)
            {
                Presets = ResizeArray(Presets, 
                                      Math.Max(address.ParameterGroupColumn+1, Presets.GetLength(0)), 
                                      Math.Max(address.SceneRow+1, Presets.GetLength(1)));
            }

            Presets[address.ParameterGroupColumn, address.SceneRow] = preset;
        }

        protected T[,] ResizeArray<T>(T[,] original, int x, int y)
        {
            T[,] newArray = new T[x, y];
            int minX = Math.Min(original.GetLength(0), newArray.GetLength(0));
            int minY = Math.Min(original.GetLength(1), newArray.GetLength(1));

            for (int i = 0; i < minY; ++i)
                Array.Copy(original, i * original.GetLength(0), newArray, i * newArray.GetLength(0), minX);

            return newArray;
        }

        public struct PresetAddress
        {
            public PresetAddress(int groupIndex, int sceneIndex, bool isValid = true)
            {
                ParameterGroupColumn = groupIndex;
                SceneRow = sceneIndex;
                IsValid = isValid;
            }

            public int ParameterGroupColumn;
            public int SceneRow;
            public bool IsValid;

            public bool IsValidForConfig(PresetConfiguration config)
            {
                return ParameterGroupColumn >= 0
                       && SceneRow >= 0
                       && ParameterGroupColumn < config.Presets.GetLength(0)
                       && SceneRow < config.Presets.GetLength(1);
            }
            
            public static PresetAddress NotAnAddress = new PresetAddress(-1,-1,false);
        }
    }

    public class PresetScene
    {
    }

    public class ParameterGroup
    {
    }

    public class Preset
    {
        public bool IsPlaceholder;
    }
}