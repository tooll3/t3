using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_e1c294b5_1ea8_435e_a437_26d280d3c2f4
{
    public class GodRays : Instance<GodRays>
    {
        [Output(Guid = "28bf4abe-e9a9-4302-bcca-67a6957b43a7")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "c94e4bb6-d6fc-4184-bd71-3563f4416413")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "bba257ae-e45d-43b6-9905-52e526583978")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> DepthBuffer = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "500549e0-aa60-4555-b82a-a829568959ff")]
        public readonly InputSlot<float> Amount = new InputSlot<float>();

        [Input(Guid = "b9ca7291-1f78-47e2-8ba9-35ccb282833c")]
        public readonly InputSlot<float> Size = new InputSlot<float>();

        [Input(Guid = "59568136-2903-4fd5-a504-8a6308a2a2dc")]
        public readonly InputSlot<int> Samples = new InputSlot<int>();

        [Input(Guid = "71acc14a-e26a-4412-9556-d88e0ec70e63")]
        public readonly InputSlot<float> Offset = new InputSlot<float>();

        [Input(Guid = "c59b6588-9e39-49ed-99b7-632e5f2b3bf4")]
        public readonly InputSlot<float> Rays = new InputSlot<float>();

        [Input(Guid = "6063bb2b-2255-4a0b-a222-7b8dd8397918")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "6d985988-3107-4480-8852-f17fe1c6d002")]
        public readonly InputSlot<float> ShiftDepth = new InputSlot<float>();

        [Input(Guid = "da242b1b-3cea-40e4-a4eb-f4517ebc930d")]
        public readonly InputSlot<float> Contrast = new InputSlot<float>();

        [Input(Guid = "2e487d56-cc02-42d6-978e-aeb776516118")]
        public readonly InputSlot<int> BlurSamples = new InputSlot<int>();

        [Input(Guid = "9529d9b2-40ca-4f30-9142-1338dc4e07ff")]
        public readonly InputSlot<float> BlurSize = new InputSlot<float>();

        [Input(Guid = "5129404e-54c5-4c5a-9736-69361bb3077d")]
        public readonly InputSlot<float> BlurOffset = new InputSlot<float>();

        [Input(Guid = "58c08101-ac16-4aed-aa84-259853734116")]
        public readonly InputSlot<System.Numerics.Vector3> Position = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "b64c4ece-8b59-4b32-a5fe-b99006286987")]
        public readonly InputSlot<Object> CameraReference = new InputSlot<Object>();

        [Input(Guid = "e532e889-0feb-4593-8375-c1ac8346d7ed")]
        public readonly InputSlot<SharpDX.Size2> Resolution = new InputSlot<SharpDX.Size2>();
    }
}
