using System.Numerics;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Rendering;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_5390ae74_a51e_4d10_adf7_78a8ddf3cd61
{
    public class SetDirectionalLight : Instance<SetDirectionalLight>
    {
        [Output(Guid = "7e87d7de-74c0-4dad-9ea9-d8790f3fff0f")]
        public readonly Slot<Command> Output = new Slot<Command>();

        public SetDirectionalLight()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var pointLights = context.PointLights;
            var light = new LightDefinition(Position.GetValue(context),
                                       Intensity.GetValue(context),
                                       Color.GetValue(context),
                                       Range.GetValue(context),
                                       Decay.GetValue(context));
            pointLights.Push(light);

            Command.GetValue(context); // Evaluate sub-tree

            pointLights.Pop();
        }

        [Input(Guid = "d4634e0a-1b7d-417e-82c8-9ac1880dac74")]
        public readonly InputSlot<Command> Command = new();

        [Input(Guid = "02ea5568-2d6c-4c4e-9923-b7d7c159606a")]
        public readonly InputSlot<System.Numerics.Vector3> Position = new();

        [Input(Guid = "1618CECB-6312-4E41-9770-189F9047F277")]
        public readonly InputSlot<System.Numerics.Vector3> Rotation = new();

        [Input(Guid = "5f638620-e513-4773-81df-3dac3fdf81de")]
        public readonly InputSlot<float> Intensity = new();

        [Input(Guid = "58883f45-e5f5-4ca9-aa5d-28dc3ab2c855")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new();

        [Input(Guid = "6cf5d82f-0861-40a1-9b31-5552e6c8d505")]
        public readonly InputSlot<float> Range = new();

        [Input(Guid = "45c969bb-3472-4eab-acc5-bc75466daf5d")]
        public readonly InputSlot<float> Decay = new();

        // [Input(Guid = "B05C8254-73C2-473E-8F07-4A46D1B6AE55")]
        // public readonly InputSlot<Vector2> ShadowRange = new();


    }
}