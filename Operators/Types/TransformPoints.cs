using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_7f6c64fe_ca2e_445e_a9b4_c70291ce354e
{
    public class TransformPoints : Instance<TransformPoints>, ITransformable
    {

        
        
        [Output(Guid = "ba17981e-ef9f-46f1-a653-6d50affa8838")]
        public readonly TransformCallbackSlot<T3.Core.DataTypes.BufferWithViews> Output = new TransformCallbackSlot<T3.Core.DataTypes.BufferWithViews>();

        public TransformPoints()
        {
            Output.TransformableOp = this;
        }        
        // implementation of ITransformable
        System.Numerics.Vector3 ITransformable.Translation { get => Translation.Value; set => Translation.SetTypedInputValue(value); }
        System.Numerics.Vector3 ITransformable.Rotation { get => Rotation.Value; set => Rotation.SetTypedInputValue(value); }
        System.Numerics.Vector3 ITransformable.Scale { get => Scale.Value; set => Scale.SetTypedInputValue(value); }

        public Action<ITransformable, EvaluationContext> TransformCallback { get => Output.TransformCallback; set => Output.TransformCallback = value; }
        
        [Input(Guid = "565ff364-c3d9-4c60-a9a0-79fdd36d3477")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "9e803bd1-c5a3-4f6f-926d-d19f32dcbae5")]
        public readonly InputSlot<System.Numerics.Vector3> Translation = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "454d0150-dac4-41b2-83f8-d1ecc3ef76d4")]
        public readonly InputSlot<System.Numerics.Vector3> Rotation = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "523b7acd-d8e7-4473-9ec7-15eec1d795df")]
        public readonly InputSlot<System.Numerics.Vector3> Scale = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "a6e5770b-39dc-4d7b-b92e-53302dc89395")]
        public readonly InputSlot<float> UniformScale = new InputSlot<float>();

        [Input(Guid = "0192b746-ff90-4c26-a7d4-754b6ec8006b")]
        public readonly InputSlot<bool> UpdateRotation = new InputSlot<bool>();
        
        
        
        
        
        
        
        
        
    }
}

