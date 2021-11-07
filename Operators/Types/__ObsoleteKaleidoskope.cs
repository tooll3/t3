using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_d1570f7a_dd4d_4ca3_821b_b0bc7efdf487
{
    public class __ObsoleteKaleidoskope : Instance<__ObsoleteKaleidoskope>
    {
        [Output(Guid = "3bae278b-2555-43a8-8837-e164c87a0900")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();


        [Input(Guid = "108890d5-be69-424c-aa35-1e1b6d719cf6")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "a3a488ef-03de-403e-a22f-e22d7a5affae")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();

        [Input(Guid = "fbc05ff5-4688-4dd9-8879-92608993a7b4")]
        public readonly InputSlot<System.Numerics.Vector2> Center = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "b9a907aa-fa9d-43cb-adb3-c2e5e2aa46e9")]
        public readonly InputSlot<System.Numerics.Vector2> Offset = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "ba0e780f-7016-4701-b2b1-735517eb3d03")]
        public readonly InputSlot<float> Angle = new InputSlot<float>();

        [Input(Guid = "9bded741-c80a-49ad-9c71-4f1c2b59ee56")]
        public readonly InputSlot<float> AngleOffset = new InputSlot<float>();

        [Input(Guid = "2b37c4b0-9316-4d76-99da-08a2bb97a4dc")]
        public readonly InputSlot<int> Steps = new InputSlot<int>();

        [Input(Guid = "9dd75b5c-152d-4cd3-9568-8562639e242c")]
        public readonly InputSlot<float> Fade = new InputSlot<float>();
    }
}

