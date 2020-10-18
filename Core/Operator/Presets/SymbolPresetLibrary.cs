using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core.Logging;
using T3.Core.Operator.Slots;

namespace T3.Core.Operator.Presets
{
    /// <summary>
    /// Manages definition and application of a presets for a symbol 
    /// </summary>
    public class SymbolPresetLibrary
    {
        internal SymbolPresetLibrary(Guid symbolId)
        {
            _symbolId = symbolId;
        }
        
        internal void SaveInputValue(Instance instance, SymbolChild.Input input)
        {
            // Add reference
            var childReference = ChildPresetReferences.SingleOrDefault(r => r.SymbolChildId == instance.SymbolChildId);
            if (childReference == null)
            {
                childReference = new ChildPresetReference(instance.SymbolChildId);
                ChildPresetReferences.Add(childReference);
            }

            var drivenInput = childReference.DrivenInputs.SingleOrDefault(p => p.Id == input.InputDefinition.Id);
            if (drivenInput == null)
            {
                childReference.DrivenInputs.Add(input.InputDefinition);
            }

            // Add preset value
            var preset = GetOrCreateCurrentPreset();
            var presetValue = preset.PresetValues.SingleOrDefault(pvalue => pvalue.SymbolChildId == instance.SymbolChildId && pvalue.InputId == input.InputDefinition.Id);
            if (presetValue == null)
            {
                presetValue = new PresetValue(instance.SymbolChildId, input.InputDefinition.Id); 
                preset.PresetValues.Add(presetValue);
            }

            presetValue.InputValue = input.Value.Clone();
        }

        
        public void ApplyPreset(Instance composition, int index)
        {
            if (!PresetsByIndex.TryGetValue(index, out var preset))
            {
                Log.Error("Can't find preset " + index);
                return;
            }

            foreach (var presetValue in preset.PresetValues)
            {
                var child = composition.Children.Single(c => c.SymbolChildId == presetValue.SymbolChildId);
                var inputSlot = child.Inputs.Single(i => i.Id == presetValue.InputId);
                if (presetValue.InputValue != null)
                {
                    if (inputSlot is InputSlot<float> floatInput
                        && presetValue.InputValue is InputValue<float> floatValue)
                    {
                        floatInput.TypedInputValue.Value = floatValue.Value;
                        floatInput.DirtyFlag.Invalidate();
                    }
                }
            }
        }
        
        
        public Preset AddNewPreset()
        {
            var nextIndex = PresetsByIndex.Keys.Max() + 1;
            return GetOrCreatePreset(nextIndex);
        }

        public Preset GetOrCreateCurrentPreset()
        {
            return GetOrCreatePreset(CurrentPresetIndex);
        }
        
        public Preset GetOrCreatePreset(int index)
        {
            PresetsByIndex.TryGetValue(index, out var preset);
            if (preset == null)
            {
                preset = new Preset() { Index = index};
                foreach (var childRef in ChildPresetReferences)
                {
                    var instance = SymbolRegistry.Entries[_symbolId].Children.Single(child => child.Id == childRef.SymbolChildId);
                    
                    foreach (var param in childRef.DrivenInputs)
                    {
                        preset.PresetValues.Add(new PresetValue(childRef.SymbolChildId, param.Id)
                                                    {
                                                        InputValue = instance.InputValues[param.Id].Value.Clone()
                                                    });                        
                    }
                }
                PresetsByIndex[index] = preset;
            }

            return preset;
        }
        
        private Guid _symbolId;

        public readonly Dictionary<int, Preset> PresetsByIndex = new Dictionary<int, Preset>();

        /// <summary>
        /// We assume that there could be multiple instance of the same Symbol within a Symbol. E.g. multiple Blobs.
        /// Each of these Symbols could have another current preset index. We also assume that distinguishing between
        /// difference instances is not required. 
        /// </summary>
        //public Dictionary<Guid, int> CurrentPresetIndexForSymbolChild = new Dictionary<Guid, int>();
        public int CurrentPresetIndex;

        public readonly List<ChildPresetReference> ChildPresetReferences = new List<ChildPresetReference>();

        public class ChildPresetReference
        {
            public ChildPresetReference(Guid symbolChildId)
            {
                SymbolChildId = symbolChildId;
            }
            
            public Guid SymbolChildId;
            public List<Symbol.InputDefinition> DrivenInputs = new List<Symbol.InputDefinition>();
        }

        public class Preset
        {
            public int Index;
            public List<PresetValue> PresetValues = new List<PresetValue>();
        }

        public class PresetValue
        {
            public PresetValue(Guid symbolChildId, Guid inputId)
            {
                SymbolChildId = symbolChildId;
                InputId = inputId;
            }
            
            public InputValue InputValue;
            public Guid SymbolChildId;
            public Guid InputId;
        }
    }
}