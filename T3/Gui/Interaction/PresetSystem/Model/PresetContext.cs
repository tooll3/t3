using System;
using System.Collections.Generic;
using System.Linq;

namespace T3.Gui.Interaction.PresetSystem.Model
{
    /// <summary>
    /// Model of a single composition
    /// </summary>
    public class PresetContext
    {
        public Guid CompositionId = Guid.Empty;
        public readonly List<PresetScene> Scenes = new List<PresetScene>();
        public readonly List<ParameterGroup> Groups = new List<ParameterGroup>();
        public Preset[,] Presets = new Preset[4, 4];
        public PresetAddress ViewWindow;
        

        
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
        
        public PresetScene CreateSceneAt(PresetAddress address, bool makeActive= true)
        {
            var newScene = new PresetScene();
            
            while (Scenes.Count <= address.SceneRow)
            {
                Scenes.Add(null);
            }

            Scenes[address.SceneRow] = newScene;
            ActiveGroupId = newScene.Id;
            return new PresetScene();
        }

        #endregion
        

        //----------------------------------------------------------------------------------------
        #region groups
        public Guid ActiveGroupId = Guid.Empty;
        public ParameterGroup ActiveGroup => Groups.SingleOrDefault(g => g.Id == ActiveGroupId);

        public ParameterGroup GetGroupAtIndex(int index)
        {
            return Groups.Count <= index ? null : Groups[index];
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
        private T[,] ResizeArray<T>(T[,] original, int x, int y)
        {
            T[,] newArray = new T[x, y];
            var minX = Math.Min(original.GetLength(0), newArray.GetLength(0));
            var minY = Math.Min(original.GetLength(1), newArray.GetLength(1));

            for (var i = 0; i < minY; ++i)
                Array.Copy(original, i * original.GetLength(0), newArray, i * newArray.GetLength(0), minX);

            return newArray;
        }

        /// <summary>
        /// Maps a button to an correct address by applying view window   
        /// </summary>
        public PresetAddress GetAddressFromButtonIndex(int buttonRangeIndex, int columnCount = 8)
        {
            var localAddress = new PresetAddress(buttonRangeIndex % columnCount, buttonRangeIndex / columnCount);
            return localAddress - ViewWindow;
        }
        #endregion
        
    }
}