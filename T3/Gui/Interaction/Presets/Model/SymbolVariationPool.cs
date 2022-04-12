using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Interaction.Variation;
using T3.Gui.Interaction.Variation.Model;

namespace t3.Gui.Interaction.Presets.Model
{
    /// <summary>
    /// Collects all presets and variations for a symbol 
    /// </summary>
    public class SymbolVariations
    {
        public Guid SymbolId;
        public List<SymbolVariation> Variations;
        public List<SymbolVariation> Presets;

        public static SymbolVariations InitSymbolVariations(Guid compositionId)
        {
            
        }
        
        public static List<SymbolVariation> SymbolVariations ReadFromJson(Guid compositionId)
        {
            var filepath = GetFilepathForCompositionId(compositionId);
            if (!File.Exists(filepath))
            {
                //Log.Error($"Could not find symbol file containing the id '{compositionId}'");
                return null;
            }

            Log.Info($"Reading presets definition for : {compositionId}");

            using var sr = new StreamReader(filepath);
            using var jsonReader = new JsonTextReader(sr);
            SymbolVariations newVariations = null;
                
            try
            {

                //Json json = new Json { Reader = jsonReader };
                var jToken = JToken.ReadFrom(jsonReader);

                newVariations = new SymbolVariations()
                                         {
                                             SymbolId = Guid.Parse(jToken["Id"].Value<string>()),
                                         };
                
                newVariations.;
        public int ActivationIndex;
        public bool IsActivated;
        public DateTime PublishedDate;                

                // Presets
                {
                    var groupCount = jToken.Value<int>("GroupCount");
                    var sceneCount = jToken.Value<int>("SceneCount");
                    var presetIndex = 0;
                    var jsonPresets = (JArray)jToken["Presets"];

                    newOpVariation.Presets = new Preset[groupCount, sceneCount];

                    for (var groupIndex = 0; groupIndex < groupCount; groupIndex++)
                    {
                        for (var sceneIndex = 0; sceneIndex < sceneCount; sceneIndex++)
                        {
                            var presetToken = jsonPresets[presetIndex];
                            newOpVariation.Presets[groupIndex, sceneIndex] = presetToken.HasValues
                                                                                 ? Preset.FromJson(presetToken)
                                                                                 : null;

                            presetIndex++;
                        }
                    }
                }

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
            }
            catch (Exception e)
            {
                Log.Error($"Failed to load presets and variations for {compositionId}: {e.Message}");
                return null;
            }


            return newOpVariation;
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

    public class SymbolVariation
    {
        public Guid Id;
        public string Title;
        public int ActivationIndex;
        public bool IsActivated;
        public DateTime PublishedDate;

        /// <summary>
        /// Changes by SymbolChildId
        /// </summary>
        public Dictionary<Guid, Dictionary<Guid, InputValue>> InputValuesForChildIds;
    }
    
    
}