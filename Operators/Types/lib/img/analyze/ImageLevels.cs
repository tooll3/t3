using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_592a2b6f_d4e3_43e0_9e73_034cca3b3900
{
    public class ImageLevels : Instance<ImageLevels>
    {
        [Output(Guid = "ae9ebfa0-3528-489b-9c07-090f26dd6968")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output = new();

        [Input(Guid = "f434bac8-b7d8-4787-adf2-1782d6588da8")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture2d = new();

        [Input(Guid = "1224b62e-5fca-41e9-a388-4c13c1458d56")]
        public readonly InputSlot<System.Numerics.Vector2> Center = new();

        [Input(Guid = "48e80f45-9685-4ded-aa1c-d05e16658c5a")]
        public readonly InputSlot<float> Width = new();

        [Input(Guid = "f1084d72-f8b8-4723-82be-e1e98880faf3")]
        public readonly InputSlot<float> Rotation = new();

        [Input(Guid = "a8a4d660-7356-40de-8dc6-549a72b69973")]
        public readonly InputSlot<float> ShowOriginal = new();
    }
}

