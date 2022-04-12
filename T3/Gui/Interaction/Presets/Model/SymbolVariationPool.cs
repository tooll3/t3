using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Logging;
using T3.Core.Operator;

namespace t3.Gui.Interaction.Presets.Model
{
    /// <summary>
    /// Collects all presets and variations for a symbol 
    /// </summary>
    public class SymbolVariationPool
    {
        public Guid SymbolId;
        public List<Variation2> Variations;
        public List<Variation2> Presets;

        public static SymbolVariationPool InitVariationPoolForSymbol(Guid compositionId)
        {
            var newPool = new SymbolVariationPool()
                              {
                                  SymbolId = compositionId
                              };

            newPool.Variations = LoadVariations(compositionId, VariationType.SymbolVariation);
            newPool.Presets = LoadVariations(compositionId, VariationType.Preset);
            return newPool;
        }

        private enum VariationType
        {
            Preset,
            SymbolVariation,
        }
        
        private static List<Variation2> LoadVariations(Guid compositionId, VariationType variationType)
        {
            
            var filepath = variationType == VariationType.SymbolVariation
                               ? $".t3/Variations/{compositionId}.var"
                               : $".Presets/{compositionId}.var";

            if (!File.Exists(filepath))
            {
                //Log.Error($"Could not find symbol file containing the id '{compositionId}'");
                return null;
            }

            Log.Info($"Reading presets definition for : {compositionId}");

            using var sr = new StreamReader(filepath);
            using var jsonReader = new JsonTextReader(sr);

            var result = new List<Variation2>();

            try
            {
                var jToken = JToken.ReadFrom(jsonReader);
                foreach (var sceneToken in (JArray)jToken["Variations"])
                {
                    if (sceneToken == null)
                    {
                        Log.Error("No variations?");
                        continue;
                    }
                        
                    var newVariation = Variation2.FromJson(sceneToken);
                    if (newVariation == null)
                    {
                        Log.Warning($"Failed to parse variation json:" + sceneToken);
                        continue;
                    }

                    newVariation.IsPreset = variationType == VariationType.Preset;
                    result.Add(newVariation);
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed to load presets and variations for {compositionId}: {e.Message}");
                return null;
            }

            return result;

            // newOpVariation.Presets = new Preset[groupCount, sceneCount];
            //
            // for (var groupIndex = 0; groupIndex < groupCount; groupIndex++)
            // {
            //     for (var sceneIndex = 0; sceneIndex < sceneCount; sceneIndex++)
            //     {
            //         var presetToken = jsonPresets[presetIndex];
            //         newOpVariation.Presets[groupIndex, sceneIndex] = presetToken.HasValues
            //                                                              ? Preset.FromJson(presetToken)
            //                                                              : null;
            //
            //         presetIndex++;
            //     }
            // }

            // // Groups
            // foreach (var groupToken in (JArray)jToken["Groups"])
            // {
            //     newOpVariation.Groups.Add(ParameterGroup.FromJson(groupToken));
            // }
            //
            // // Scene
            // foreach (var sceneToken in (JArray)jToken["Scenes"])
            // {
            //     //newOpVariation.Scenes.Add(PresetScene.FromJson(sceneToken));
            // }

            //return newOpVariation;
        }

        private static string GetFilepathForCompositionId(Guid id)
        {
            return PresetFolderPath + GetFilenameForCompositionId(id);
        }

        public static string PresetFolderPath = ".Presets/";
        public static string UserVariationsFolderPath = ".t3/UserVariations/";

        private static string GetFilenameForCompositionId(Guid id)
        {
            return $"{id}_variations.json";
        }
    }

    /// <summary>
    /// Base class for serialization 
    /// </summary>
    public class Variation2
    {
        public Guid Id;
        public string Title;
        public int ActivationIndex;
        public bool IsPreset;
        public DateTime PublishedDate;

        //public static abstract string GetFileFolder();
        public static string FileFolder;

        /// <summary>
        /// Changes by SymbolChildId
        /// </summary>
        public Dictionary<Guid, Dictionary<Guid, InputValue>> InputValuesForChildIds;

        public static Variation2 FromJson(JToken jToken)
        {
            var newVariation = new Variation2
                                   {
                                       Id = Guid.Parse(jToken["Id"].Value<string>()),
                                   };

            newVariation.InputValuesForChildIds = new Dictionary<Guid, Dictionary<Guid, InputValue>>();

            var changesToken = (JObject)jToken["InputValuesForChildIds"];
            if (changesToken == null)
                return newVariation;
            
            foreach (var (symbolChildIdString, changes2)  in changesToken)
            {
                var changeList = new Dictionary<Guid, InputValue>();
                if (changes2 is not JObject o)
                    continue;
                    
                foreach (var (inputIdString, change)  in o)
                {
                    var inputId = Guid.Parse(inputIdString);
                    Log.Debug($"found value for input {inputId}: {change}");
                    changeList[inputId] = null;
                }

                if (changeList.Count > 0)
                {
                    var symbolId = Guid.Parse(symbolChildIdString);
                    newVariation.InputValuesForChildIds[symbolId] = changeList;
                }
            }

            return newVariation;
        }
    }

    // public class SymbolPreset : Variation2
    // {
    //     //public new string FileFolder = ".Presets/";
    // }
    //
    // public class SymbolVariation : Variation2
    // {
    //     //public new string FileFolder = ".t3/Variations";
    // }
}