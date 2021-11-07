using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Operators.Types.Id_fd9bffd3_5c57_462f_8761_85f94c5a629b;

namespace T3.Operators.Types.Id_7f69a5e5_28e5_44c1_b3e3_74b05faa0531
{
    public class DrawRayLines : Instance<DrawRayLines>
    {
        [Output(Guid = "2f62657b-0f4b-458b-b504-0e9dc6b29dcb")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "fd05097a-2842-464a-b8d4-1479adb7785d")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "c7f51f64-e473-4780-8659-56e85b9ed219")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "20dfe824-f1d1-4eb2-a9cb-a3240195360a")]
        public readonly InputSlot<float> Size = new InputSlot<float>();

        [Input(Guid = "7340e3bd-b1d2-4aa3-b8d0-0e941751b211")]
        public readonly InputSlot<float> ShrinkWithDistance = new InputSlot<float>();

        [Input(Guid = "e78179bf-6ba9-4eda-9989-c9de16d2db62")]
        public readonly InputSlot<float> TransitionProgress = new InputSlot<float>();

        [Input(Guid = "72db5734-f2bc-445f-9d96-61a4fb81995c")]
        public readonly InputSlot<float> UseWForWidth = new InputSlot<float>();

        [Input(Guid = "14497750-82ec-40a9-b38b-b62813ee93dc")]
        public readonly InputSlot<bool> UseWAsTexCoordV = new InputSlot<bool>();

        [Input(Guid = "47667ddc-1bbb-4686-9144-ce218172b5ec")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture_ = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "f59b07c9-f947-4ab5-96c7-20801aca8d1a")]
        public readonly InputSlot<bool> EnableTest = new InputSlot<bool>();

        [Input(Guid = "d8c585bc-8bab-412f-94bf-dc6d8d5643b3")]
        public readonly InputSlot<bool> EnableDepthWrite = new InputSlot<bool>();

        [Input(Guid = "037a162b-2054-44b1-b536-b532ab0c14b7")]
        public readonly InputSlot<int> BlendMod = new InputSlot<int>();
    }
}

