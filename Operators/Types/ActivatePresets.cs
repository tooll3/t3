using System;
using System.Collections.Generic;
using System.Diagnostics;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_a53f3873_a5aa_4bcc_aa06_0745d98209d6
{
    public class ActivatePresets : Instance<ActivatePresets>
    {
        [Output(Guid = "0F56E725-00F5-4EE9-A795-90E1DDF77A70")]
        public readonly Slot<Command> Result = new Slot<Command>();

        public ActivatePresets()
        {
            Result.UpdateAction = Update;
            
        }

        private BlendSetting _blendSetting = new BlendSetting();
        private bool _initialized;
        
        public class BlendSetting
        {
            public bool WasActivatedLastFrame;
            public int GroupIndex;
            public int PresetAIndex;
            public int PresetBIndex;
            public float BlendFactor;
        }
        
        public static Dictionary<Guid, BlendSetting>  BlendSettingForCompositionIds = new Dictionary<Guid,BlendSetting>(); 
        
        private void Update(EvaluationContext context)
        {
            if (!_initialized)
            {
                if (Parent?.Symbol == null)
                {
                    Log.Warning("Can't register Preset blending for undefined parent", this.SymbolChildId);
                    return;
                }
                BlendSettingForCompositionIds[Parent.Symbol.Id] = _blendSetting;
                _initialized = true;
            }
            
            // Evaluate subtree
            SubTree.GetValue(context);

            var wasUpdated = false;
            var groupIndex = GroupIndex.GetValue(context);
            if (groupIndex != _blendSetting.GroupIndex)
            {
                wasUpdated = true;
                _blendSetting.GroupIndex = groupIndex;
            }

            var presetA = PresetA.GetValue(context);
            if (presetA != _blendSetting.PresetAIndex)
            {
                wasUpdated = true;
                _blendSetting.PresetAIndex = presetA;
            }

            var presetB = PresetB.GetValue(context);
            if (presetB != _blendSetting.PresetBIndex)
            {
                wasUpdated = true;
                _blendSetting.PresetBIndex = presetB;
            }

            var blendFactor = BlendFactor.GetValue(context);
            if (Math.Abs(blendFactor - _blendSetting.BlendFactor) > 0.001f)
            {
                wasUpdated = true;
                _blendSetting.BlendFactor = blendFactor;
            }

            _blendSetting.WasActivatedLastFrame = wasUpdated;
        }

        [Input(Guid = "B4612399-6802-4A4D-96AC-E1496E784795")]
        public readonly InputSlot<Command> SubTree = new InputSlot<Command>();
        
        [Input(Guid = "DCFF59C3-0EE5-4DB9-89ED-D6C6E23C9DD0")]
        public readonly InputSlot<bool> IsEnabled = new InputSlot<bool>();

        [Input(Guid = "0B0EB01B-F165-42B7-898D-1A1750C23BCD")]
        public readonly InputSlot<int> GroupIndex = new InputSlot<int>();

        [Input(Guid = "97C73F45-4C94-4CC7-B602-3E70A473DCE8")]
        public readonly InputSlot<int> PresetA = new InputSlot<int>();

        [Input(Guid = "7DA346C3-8A2E-4F31-ABEF-7FC2A3C63874")]
        public readonly InputSlot<int> PresetB = new InputSlot<int>();

        [Input(Guid = "BF765765-2F00-4830-AAAA-9F0A157C6268")]
        public readonly InputSlot<float> BlendFactor = new InputSlot<float>();
    }
}