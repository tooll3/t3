using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;
using T3.Core.Logging;

namespace T3.Gui.Interaction.PresetSystem.Model
{
    /// <summary>
    /// Model of a single composition
    /// </summary>
    public class CompositionContext
    {
        public Guid CompositionId = Guid.Empty;
        public readonly List<PresetScene> Scenes = new List<PresetScene>();
        public readonly List<ParameterGroup> Groups = new List<ParameterGroup>();
        public Preset[,] Presets = new Preset[4, 4];
        public PresetAddress ViewWindow;
        public bool IsGroupExpanded { get; set; }

        //----------------------------------------------------------------------------------------
        #region scenes
        public Guid ActiveSceneId = Guid.Empty;
        public PresetScene ActiveScene => Scenes.SingleOrDefault(scene => scene.Id == ActiveGroupId);

        public PresetScene GetSceneAt(PresetAddress address)
        {
            return address.SceneRow >= Scenes.Count
                       ? null
                       : Scenes[address.GroupColumn];
        }

        public PresetScene CreateSceneAt(PresetAddress address, bool makeActive = true)
        {
            var newScene = new PresetScene();

            while (Scenes.Count <= address.SceneRow)
            {
                Scenes.Add(null);
            }

            Scenes[address.SceneRow] = newScene;
            ActiveSceneId = newScene.Id;
            return new PresetScene();
        }
        #endregion

        //----------------------------------------------------------------------------------------
        #region groups
        public Guid ActiveGroupId = Guid.Empty;

        public int ActiveGroupIndex
        {
            get
            {
                if (ActiveGroupId == Guid.Empty)
                    return -1;

                for (var i = 0; i < Groups.Count; i++)
                {
                    if (Groups[i].Id == ActiveGroupId)
                        return i;
                }

                return -1;
            }
        }

        public ParameterGroup ActiveGroup => Groups.SingleOrDefault(g => g.Id == ActiveGroupId);

        public void SetGroupAsActive(ParameterGroup group)
        {
            if (!Groups.Exists(g => g.Id == group.Id))
            {
                Log.Error("Can't set id with unknown id as active");
            }

            ActiveGroupId = group.Id;
        }

        public ParameterGroup GetGroupAtIndex(int index)
        {
            return (index < 0 || index >= Groups.Count) ? null : Groups[index];
        }

        public ParameterGroup GetGroupForAddress(PresetAddress address)
        {
            return address.GroupColumn >= Groups.Count
                       ? null
                       : Groups[address.GroupColumn];
        }

        public ParameterGroup AppendNewGroup(string nameForNewGroup)
        {
            var newGroup = new ParameterGroup()
                               {
                                   Title = nameForNewGroup,
                                   Parameters = new List<GroupParameter>(),
                               };

            // Append or insert
            var freeSlotIndex = Groups.IndexOf(null);
            if (freeSlotIndex == -1)
            {
                Groups.Add(newGroup);
            }
            else
            {
                Groups[freeSlotIndex] = newGroup;
            }

            return newGroup;
        }

        public IEnumerable<Preset> GetPresetsForGroup(ParameterGroup group)
        {
            var groupIndex = GetIndexForGroup(group);
            if (groupIndex == -1)
            {
                yield break;
            }

            for (int sceneIndex = 0; sceneIndex < Presets.GetLength(1); sceneIndex++)
            {
                var preset = Presets[groupIndex, sceneIndex];
                if (preset == null)
                    continue;
                yield return preset;
            }
        }

        public int GetIndexForGroup(ParameterGroup group)
        {
            return Groups.IndexOf(group);
        }
        
        public void PurgeNullParametersInGroup(ParameterGroup paramGroup)
        {
            var obsoleteParamIndices = new List<int>();
            var validParamIds = new HashSet<Guid>();
            
            for (var parameterIndex = paramGroup.Parameters.Count - 1; parameterIndex >= 0; parameterIndex--)
            {
                var p = paramGroup.Parameters[parameterIndex];
                if (p != null)
                {
                    validParamIds.Add(p.Id);
                    continue;
                } 
                
                obsoleteParamIndices.Add(parameterIndex);
            }

            foreach (var i in obsoleteParamIndices)
            {
                paramGroup.Parameters.RemoveAt(i);
            }
            
            foreach (var preset in Presets)
            {
                if (preset == null)
                    continue;
                
                foreach (var parameterId in preset.ValuesForGroupParameterIds.Keys.ToList())
                {
                    if (validParamIds.Contains(parameterId))
                        continue;
                    
                    preset.ValuesForGroupParameterIds.Remove(parameterId);
                }
            }

            WriteToJson();
        }
        
        #endregion

        //----------------------------------------------------------------------------------------
        #region presets
        public Preset TryGetPresetAt(PresetAddress address)
        {
            return !address.IsValidForContext(this)
                       ? null
                       : Presets[address.GroupColumn, address.SceneRow];
        }

        public void SetPresetAt(Preset preset, PresetAddress address)
        {
            var needToExtendGrid = !address.IsValidForContext(this);
            if (needToExtendGrid)
            {
                Presets = ResizeArray(Presets,
                                      Math.Max(address.GroupColumn + 1, Presets.GetLength(0)),
                                      Math.Max(address.SceneRow + 1, Presets.GetLength(1)));
            }

            Presets[address.GroupColumn, address.SceneRow] = preset;
        }
        #endregion

        //----------------------------------------------------
        #region grip helpers
        /// <summary>
        /// Extends the size of a two dimensional array.
        /// </summary>
        /// <remarks>
        /// Using a for-loop to copy the original values is BAD.
        /// However using array.Copy lead to inconsistency and data loss
        /// </remarks>
        private static T[,] ResizeArray<T>(T[,] original, int newSizeX, int newSizeY)
        {
            const int x = 0;
            const int y = 1;

            T[,] newArray = new T[newSizeX, newSizeY];
            var minX = Math.Min(original.GetLength(x), newArray.GetLength(x));
            var minY = Math.Min(original.GetLength(y), newArray.GetLength(y));

            for (var columnIndex = 0; columnIndex < minX; columnIndex++)
            {
                for (var rowIndex = 0; rowIndex < minY; rowIndex++)
                {
                    newArray[columnIndex, rowIndex] = original[columnIndex, rowIndex];
                    //Array.Copy(original, rowIndex * original.GetLength(0), newArray, rowIndex * newArray.GetLength(0), minX);
                }
            }

            return newArray;
        }

        /// <summary>
        /// Maps a button to an correct address by applying view window   
        /// </summary>
        public PresetAddress GetAddressFromButtonIndex(int buttonRangeIndex)
        {
            var columnCount = IsGroupExpanded ? 1 : 8;
            var localAddress = new PresetAddress(
                                                 IsGroupExpanded
                                                     ? ActiveGroupIndex
                                                     : buttonRangeIndex % columnCount,
                                                 buttonRangeIndex / columnCount);
            return localAddress - ViewWindow;
        }
        #endregion

        public void WriteToJson()
        {
            var compositionId = GetFilepathForCompositionId(CompositionId);
            using (var sw = new StreamWriter(compositionId))
            using (var writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartObject();

                writer.WriteValue("Id", CompositionId);

                // Presets
                {
                    writer.WriteValue("GroupCount", Presets.GetLength(0));
                    writer.WriteValue("SceneCount", Presets.GetLength(1));
                    writer.WritePropertyName("Presets");
                    writer.WriteStartArray();
                    for (var groupIndex = 0; groupIndex < Presets.GetLength(0); groupIndex++)
                    {
                        for (var sceneIndex = 0; sceneIndex < Presets.GetLength(1); sceneIndex++)
                        {
                            writer.WriteStartObject();
                            writer.WriteComment($"preset {groupIndex}:{sceneIndex}");
                            var address = new PresetAddress(groupIndex, sceneIndex);
                            var preset = TryGetPresetAt(address);
                            preset?.ToJson(writer);

                            writer.WriteEndObject();
                        }
                    }

                    writer.WriteEndArray();
                }

                // Groups
                {
                    writer.WritePropertyName("Groups");
                    writer.WriteStartArray();
                    foreach (var @group in Groups)
                    {
                        writer.WriteStartObject();
                        @group?.ToJson(writer);
                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                }

                // Scenes
                {
                    writer.WritePropertyName("Scenes");
                    writer.WriteStartArray();
                    foreach (var scene in Scenes)
                    {
                        writer.WriteStartObject();
                        scene?.ToJson(writer);
                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }
        }

        private static string GetFilepathForCompositionId(Guid id)
        {
            return PresetPath + GetFilenameForCompositionId(id);
        }

        private static string GetFilenameForCompositionId(Guid id)
        {
            return $"{id}_presets.json";
        }

        public static CompositionContext ReadFromJson(Guid compositionId)
        {
            var filepath = GetFilepathForCompositionId(compositionId);
            if (!File.Exists(filepath))
            {
                //Log.Error($"Could not find symbol file containing the id '{compositionId}'");
                return null;
            }

            Log.Info($"Reading preset definition for : {compositionId}");

            using (var sr = new StreamReader(filepath))
            using (var jsonReader = new JsonTextReader(sr))
            {
                //Json json = new Json { Reader = jsonReader };
                var jToken = JToken.ReadFrom(jsonReader);

                var newContext = new CompositionContext()
                                     {
                                         CompositionId = Guid.Parse(jToken["Id"].Value<string>()),
                                     };

                // Presets
                {
                    var groupCount = jToken.Value<int>("GroupCount");
                    var sceneCount = jToken.Value<int>("SceneCount");
                    var presetIndex = 0;
                    var jsonPresets = (JArray)jToken["Presets"];

                    newContext.Presets = new Preset[groupCount, sceneCount];

                    for (var groupIndex = 0; groupIndex < groupCount; groupIndex++)
                    {
                        for (var sceneIndex = 0; sceneIndex < sceneCount; sceneIndex++)
                        {
                            var presetToken = jsonPresets[presetIndex];
                            newContext.Presets[groupIndex, sceneIndex] = presetToken.HasValues
                                                                             ? Preset.FromJson(presetToken)
                                                                             : null;

                            presetIndex++;
                        }
                    }
                }

                // Groups
                foreach (var groupToken in (JArray)jToken["Groups"])
                {
                    newContext.Groups.Add(ParameterGroup.FromJson(groupToken));
                }

                // Scene
                foreach (var sceneToken in (JArray)jToken["Scenes"])
                {
                    newContext.Scenes.Add(PresetScene.FromJson(sceneToken));
                }

                return newContext;
            }
        }

        protected static string PresetPath { get; } = @"Resources\presets\";
    }
}