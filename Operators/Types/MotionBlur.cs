using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_196e14c7_3a71_4afd_a53b_e51d0fe8f414
{
    public class MotionBlur : Instance<MotionBlur>
    {
        [Output(Guid = "1b237829-8cfd-4039-a6c5-8ca3dbb225f7")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "619c2684-8495-4c19-a5b2-673728feaa00")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "3d99ccde-2bc3-4a25-962d-dab4fc6c554a")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> DisplaceMap = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "27d15bd6-ca13-4e4e-acbe-d90039d59ef9")]
        public readonly InputSlot<float> Displacement = new InputSlot<float>();

        [Input(Guid = "ff43e574-eb8f-4d6a-9f89-31f48c3017f9")]
        public readonly InputSlot<float> DisplacementOffset = new InputSlot<float>();

        [Input(Guid = "913d8cbd-9109-4591-9e2c-aa88c04ceca5")]
        public readonly InputSlot<float> Twist = new InputSlot<float>();

        [Input(Guid = "66de01c4-d682-48d4-ad2b-1e57a23d2592")]
        public readonly InputSlot<float> Shade = new InputSlot<float>();

        [Input(Guid = "df8bf08c-e576-4310-95b5-e34c6a001959")]
        public readonly InputSlot<int> SampleCount = new InputSlot<int>();

        [Input(Guid = "00494ca1-9455-40fe-bb18-8dfeb34d348e")]
        public readonly InputSlot<float> DisplaceMapSampling = new InputSlot<float>();

        [Input(Guid = "052bb3cd-2cf4-4d53-a5a4-c6ba2b34e8b7")]
        public readonly InputSlot<float> SampleSpread = new InputSlot<float>();

        [Input(Guid = "a35a62bb-d023-4f55-9e1f-0b9faf316f4d")]
        public readonly InputSlot<float> SampleOffset = new InputSlot<float>();

        [Input(Guid = "d757058a-a31e-487f-b002-cc06bc478535")]
        public readonly InputSlot<Object> CameraReference = new InputSlot<Object>();

    }
}

