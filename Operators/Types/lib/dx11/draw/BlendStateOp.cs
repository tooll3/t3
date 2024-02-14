using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_064ca51f_47ab_4cb7_95f2_e537b68e137e
{
    public class BlendStateOp : Instance<BlendStateOp>
    {
        [Output(Guid = "6EF6C44B-EE22-4C64-9910-4F7595C41897")]
        public readonly Slot<BlendState> BlendState = new();

        public BlendStateOp()
        {
            BlendState.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            BlendState.Value?.Dispose();
            var blendDesc = new BlendStateDescription()
                            {
                                AlphaToCoverageEnable = AlphaToCoverageEnable.GetValue(context),
                                IndependentBlendEnable = IndependentBlendEnable.GetValue(context),
                            };

            if (RenderTargets.DirtyFlag.IsDirty)
            {
                _connectedDescriptions = RenderTargets.GetCollectedTypedInputs();
                RenderTargets.DirtyFlag.Clear();
            }

            for (int i = 0; i < _connectedDescriptions.Count; i++)
            {
                blendDesc.RenderTarget[i] = _connectedDescriptions[i].GetValue(context);
            }

            try
            {
                BlendState.Value = new BlendState(ResourceManager.Device, blendDesc); // todo: put into resource manager
            }
            catch (SharpDXException e)
            {
                Log.Error("Failed to create BlendState " + e.Message);
            } 
        }

        private List<Slot<RenderTargetBlendDescription>> _connectedDescriptions;

        [Input(Guid = "63D0E4E8-FA00-4059-A11B-6A31E66757DC")]
        public readonly MultiInputSlot<RenderTargetBlendDescription> RenderTargets = new();
        [Input(Guid = "3CA79807-00C9-471A-AC44-525A05740FED")]
        public readonly InputSlot<bool> AlphaToCoverageEnable = new();
        [Input(Guid = "873AD863-DEC6-4B4B-9D81-89D5FA11BEEC")]
        public readonly InputSlot<bool> IndependentBlendEnable = new();
    }
}