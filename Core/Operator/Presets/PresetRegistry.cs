using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core.Operator.Slots;

namespace T3.Core.Operator.Presets
{
    public class PresetRegistry
    {
        #region public API -------------------------------------
        public static void AddInputToCompositionPreset(Instance composition, Instance childInstance, SymbolChild.Input inputSlot)
        {
            var presetLib = GetOrCreatePresetLibForSymbol(composition.Symbol.Id);
            presetLib.SaveInputValue(childInstance, inputSlot);
        }

        private static SymbolPresetLibrary GetOrCreatePresetLibForSymbol(Guid symbolId)
        {
            if (PresetLibraries.TryGetValue(symbolId, out var presetLib))
            {
                return presetLib;
            }

            var newPresetLib = new SymbolPresetLibrary(symbolId);
            PresetLibraries[symbolId] = newPresetLib;
            return newPresetLib;
        }

        public static SymbolPresetLibrary TryGetPresetLibForSymbol(Guid symbolId)
        {
            PresetLibraries.TryGetValue(symbolId, out var presetLib);
            return presetLib;
        }
        
        public static void LoadAll()
        {
            // to be implemented
        }

        public static void SaveAll()
        {
            // to  be implemented
        }

        
        #endregion ----------------------------------------------



        private static Dictionary<Guid, SymbolPresetLibrary> PresetLibraries = new Dictionary<Guid, SymbolPresetLibrary>();
    }
}