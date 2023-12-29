using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Rendering;

namespace T3.Operators.Types.Id_4912ba82_460a_4229_884d_6b647d64b08c
{
    public class SetPointLight : Instance<SetPointLight>
    {
        [Output(Guid = "d8c0cd0c-90e9-44fa-9f11-dda5a5b47239", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new();

        public SetPointLight()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var pointLights = context.PointLights;
            var light = new PointLight(Position.GetValue(context),
                                       Intensity.GetValue(context),
                                       Color.GetValue(context),
                                       Range.GetValue(context),
                                       Decay.GetValue(context));
            pointLights.Push(light);

            Command.GetValue(context); // Evaluate sub-tree

            pointLights.Pop();
        }

        [Input(Guid = "d46d2cb6-9ab9-4640-a3d4-e70e643bd3b4")]
        public readonly InputSlot<Command> Command = new();

        [Input(Guid = "f46ed192-1f6c-415e-8fe3-6d4909859f6b")]
        public readonly InputSlot<System.Numerics.Vector3> Position = new();

        [Input(Guid = "747e043b-39e9-420c-8314-b7d1ba5d61de")]
        public readonly InputSlot<float> Intensity = new();

        [Input(Guid = "7b77b6fb-e8bd-42a7-8fb9-5d411f9a0da6")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new();

        [Input(Guid = "e825e0b5-4c04-4ce6-9aef-7d099e9d2430")]
        public readonly InputSlot<float> Range = new();

        [Input(Guid = "0318DCC8-3E8A-4A3E-A0FD-239B55183EE9")]
        public readonly InputSlot<float> Decay = new();
    }
}