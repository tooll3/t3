using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_697bbc2d_0b2e_4013_bbc3_d58a28a79f31
{
    public class SoftTransformPoints : Instance<SoftTransformPoints>, ITransformable
    {

        [Output(Guid = "b3309ed0-574f-4907-b477-4a1cf98b2fe5")]
        public readonly TransformCallbackSlot<T3.Core.DataTypes.BufferWithViews> Output = new TransformCallbackSlot<T3.Core.DataTypes.BufferWithViews>();

        public SoftTransformPoints()
        {
            Output.TransformableOp = this;
        }
        
        System.Numerics.Vector3 ITransformable.Translation { get => VolumePosition.Value; set => VolumePosition.SetTypedInputValue(value); }
        System.Numerics.Vector3 ITransformable.Rotation { get => System.Numerics.Vector3.Zero; set { } }
        System.Numerics.Vector3 ITransformable.Scale { get => System.Numerics.Vector3.One; set { } }

        public Action<ITransformable, EvaluationContext> TransformCallback { get => Output.TransformCallback; set => Output.TransformCallback = value; }

        
        [Input(Guid = "5fac3f09-d6dd-4cba-8575-983353e60af4")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "1055ad89-2aa1-493f-b991-ae55b7fbf2e4")]
        public readonly InputSlot<System.Numerics.Vector3> Translate = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "a867c29a-3cca-49c0-92ae-a7d094b5213b")]
        public readonly InputSlot<float> ScatterPosition = new InputSlot<float>();

        [Input(Guid = "663e5d09-da7a-447c-abdd-984cc3ef5e4a")]
        public readonly InputSlot<System.Numerics.Vector3> Scale = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "cdf7c96c-7630-4889-b7a9-4ae0c3160119")]
        public readonly InputSlot<float> ScaleMagnitude = new InputSlot<float>();

        [Input(Guid = "e1c9d413-00b9-4d5d-81b6-6fa960a159be")]
        public readonly InputSlot<System.Numerics.Vector3> RotateAxis = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "c770d786-386e-4867-920b-979f5586160b")]
        public readonly InputSlot<float> RotateAngle = new InputSlot<float>();

        [Input(Guid = "3a7828e2-f58e-4229-b6c2-636cd5dbd011")]
        public readonly InputSlot<System.Numerics.Vector3> VolumePosition = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "24a0635f-9599-4a53-a35a-de90f4719f56")]
        public readonly InputSlot<int> VolumeType = new InputSlot<int>();

        [Input(Guid = "76a7afbe-4782-4b3c-bc35-cc818cf06ab2")]
        public readonly InputSlot<System.Numerics.Vector3> VolumeSize = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "36e2d2a8-4910-4c83-b9ff-ced8df59c7f1")]
        public readonly InputSlot<float> VolumeSizeMagnitude = new InputSlot<float>();

        [Input(Guid = "1e1f40ea-15af-4191-b3ce-d2edc3eee243")]
        public readonly InputSlot<float> SoftRadius = new InputSlot<float>();

        [Input(Guid = "f98281bc-89cb-4ac7-9d27-e045e712eb3a")]
        public readonly InputSlot<float> Bias = new InputSlot<float>();

        [Input(Guid = "2cbbb3eb-7e40-4a0a-9ed9-460953384750")]
        public readonly InputSlot<float> UseWAsWeight = new InputSlot<float>();
    }
}

