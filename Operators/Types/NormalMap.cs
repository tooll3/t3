using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_01458940_287f_4d31_9906_998efa9a2641
{
    public class NormalMap : Instance<NormalMap>
    {
        [Output(Guid = "b1fa156b-a959-42f8-9a81-30a667d60554")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "3b04296a-14e5-40e0-91a2-eda0314b0490")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> LightMap = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "ab21289c-f91c-4991-a7e5-ecb0c0954f02")]
        public readonly InputSlot<float> Impact = new InputSlot<float>();

        [Input(Guid = "3e9bce35-7de1-42de-80e5-8eec29e92422")]
        public readonly InputSlot<float> SampleRadius = new InputSlot<float>();

        [Input(Guid = "b16de87a-4099-42fe-9a73-97d8fa112d4d")]
        public readonly InputSlot<SharpDX.Size2> Resolution = new InputSlot<SharpDX.Size2>();

        [Input(Guid = "82464caa-a407-4f6d-a062-cef322d131f0")]
        public readonly InputSlot<float> Twist = new InputSlot<float>();

        [Input(Guid = "ae06f89e-d5a4-43a3-957d-718b66d76918")]
        public readonly InputSlot<bool> WriteAngleAndStrength = new InputSlot<bool>();
    }
}

