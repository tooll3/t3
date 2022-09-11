using System;
using SharpDX;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types.Id_6c2f8241_9f4b_451e_8a1d_871631d21163
{
    public class ExecuteTextureUpdate : Instance<ExecuteTextureUpdate>
    {
        [Output(Guid = "C955F2A2-9823-4844-AC11-98EA07DC50AA")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();

        public ExecuteTextureUpdate()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if (TriggerTexture.IsConnected && !TriggerTexture.DirtyFlag.IsDirty)
            {
                Output.DirtyFlag.Clear();
                // UpdateCommands.DirtyFlag.Clear();
                // TriggerTexture.DirtyFlag.Clear();

                return;
            }
            
            if (UpdateCommands.IsConnected && UpdateCommands.DirtyFlag.IsDirty)
            {
                // This will execute the input
                UpdateCommands.GetValue(context);
            }
            UpdateCommands.DirtyFlag.Clear();
            TriggerTexture.DirtyFlag.Clear();
            
            var inputBuffer = Texture.GetValue(context);
            Output.Value = inputBuffer;
        }

        [Input(Guid = "088ddcee-1407-4cd8-85bc-6704b8ea73b1")]
        public readonly InputSlot<Command> UpdateCommands = new InputSlot<Command>();
        
        [Input(Guid = "5599A8AC-0686-4FA8-806C-52A44F910F11")]
        public readonly InputSlot<Texture2D> Texture = new InputSlot<Texture2D>();
        
        [Input(Guid = "DC3BD757-300B-416A-92F9-B9C976EF7206")]
        public readonly InputSlot<Texture2D> TriggerTexture = new InputSlot<Texture2D>();
    }
}