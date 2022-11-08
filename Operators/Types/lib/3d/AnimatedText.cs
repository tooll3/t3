using System;
using System.Numerics;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_1dbbb50f_98e1_45fe_bd14_f41b5940a019
{
    public class AnimatedText : Instance<AnimatedText>
,ITransformable
    {
        public enum HorizontalAligns
        {
            Left,
            Center,
            Right,
        }
        
        public enum VerticalAligns
        {
            Top,
            Middle,
            Bottom,
        }
        
        [Output(Guid = "3741ee35-9762-4280-8035-f77a9166b3c2")]
        public readonly TransformCallbackSlot<Command> Output = new TransformCallbackSlot<Command>();

        
        public AnimatedText()
        {
            Output.TransformableOp = this;
        }
        
        IInputSlot ITransformable.TranslationInput => Position;
        IInputSlot ITransformable.RotationInput => null;
        IInputSlot ITransformable.ScaleInput => null;
        public Action<Instance, EvaluationContext> TransformCallback { get; set; }

        
        [Input(Guid = "fd8fa01f-d462-43e8-a3e6-4f4263e5b87b")]
        public readonly InputSlot<string> InputText = new InputSlot<string>();

        [Input(Guid = "b7f762fd-66b0-4603-960d-b755856bdb99")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "987f8d07-4cf7-49d7-b21c-5a1c4f8a2ca8")]
        public readonly InputSlot<System.Numerics.Vector4> Shadow = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "c12e24e4-c5fb-4115-b302-388730c57ee0")]
        public readonly InputSlot<System.Numerics.Vector2> Position = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "ac7a8af7-62e1-42ef-a260-323e8013415f")]
        public readonly InputSlot<float> Size = new InputSlot<float>();

        [Input(Guid = "358df770-b3de-48be-8366-1dccc82e4962")]
        public readonly InputSlot<float> LineHeight = new InputSlot<float>();

        [Input(Guid = "39fee5f6-a1bd-47b9-b2ee-42fbcc8f05cb")]
        public readonly InputSlot<int> VerticalAlign = new InputSlot<int>();

        [Input(Guid = "74e59015-cb83-4d58-8469-c0f799a508e4")]
        public readonly InputSlot<int> HorizontalAlign = new InputSlot<int>();

        [Input(Guid = "d71ef13a-17f3-4370-99fd-5cea92114562")]
        public readonly InputSlot<float> Spacing = new InputSlot<float>();

        [Input(Guid = "756a8827-4ffd-488d-a79a-6eea6f13e3fe")]
        public readonly InputSlot<string> FontPath = new InputSlot<string>();

        [Input(Guid = "ceb6f328-1c44-4eec-96e5-a192a411a9e0")]
        public readonly InputSlot<SharpDX.Direct3D11.CullMode> CullMode = new InputSlot<SharpDX.Direct3D11.CullMode>();

        [Input(Guid = "94378474-f92d-4c07-befe-a453435f24a8")]
        public readonly InputSlot<bool> EnableZTest = new InputSlot<bool>();

        [Input(Guid = "05d35c66-39c0-4768-b761-85cfd1083e31")]
        public readonly InputSlot<float> TransitionProgress = new InputSlot<float>();

        [Input(Guid = "c8b9bdeb-356e-4a6d-91e2-e8b9f5e6edd4")]
        public readonly InputSlot<float> TransitionSpread = new InputSlot<float>();

        [Input(Guid = "58176355-6683-4334-8da7-c1c37e8e6d2b")]
        public readonly InputSlot<T3.Core.DataTypes.Gradient> ColorTransition = new InputSlot<T3.Core.DataTypes.Gradient>();

        [Input(Guid = "12f9b103-1f94-437c-891f-fa28bcf93d8d")]
        public readonly InputSlot<System.Numerics.Vector3> Movement = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "b22fb2c1-3053-48b1-a31e-58458bfc31d5")]
        public readonly InputSlot<float> RandomizeTiming = new InputSlot<float>();

        [Input(Guid = "b99561fa-c8ad-4457-9eb0-bca357a78698")]
        public readonly InputSlot<float> RandomizeMovement = new InputSlot<float>();

        [Input(Guid = "d1f0018f-b07d-4397-8fe9-c8dee9bfa32a")]
        public readonly InputSlot<float> Seed = new InputSlot<float>();

        [Output(Guid = "5a784cb1-9011-4e8f-a5d5-c1bd4cc7d2df")]
        public readonly Slot<T3.Core.DataTypes.MeshBuffers> MeshBuffer = new Slot<T3.Core.DataTypes.MeshBuffers>();
    }
}

