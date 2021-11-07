using System;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_9c67a8c8_839f_4f67_a949_08cb38b9dffd
{
    public class PointLight : Instance<PointLight>, ITransformable
    {
        [Output(Guid = "32b87a4d-bef3-4646-be76-8f8224ebd5c2")]
        public readonly TransformCallbackSlot<Command> Output = new TransformCallbackSlot<Command>();

        public PointLight()
        {
            Output.TransformableOp = this;
        }

        System.Numerics.Vector3 ITransformable.Translation { get => Position.Value; set => Position.SetTypedInputValue(value); }
        System.Numerics.Vector3 ITransformable.Rotation { get => System.Numerics.Vector3.Zero; set { } }
        System.Numerics.Vector3 ITransformable.Scale { get => System.Numerics.Vector3.One; set { } }

        public Action<ITransformable, EvaluationContext> TransformCallback { get => Output.TransformCallback; set => Output.TransformCallback = value; }

        [Input(Guid = "55dc52d8-51a6-497a-9624-b118e0e27c65")]
        public readonly InputSlot<Command> Command = new InputSlot<Command>();

        [Input(Guid = "f6d96a01-dc90-49c7-9152-a6a42bb05218")]
        public readonly InputSlot<System.Numerics.Vector3> Position = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "98155900-1bb9-427a-9c4e-0988fec806cd")]
        public readonly InputSlot<float> Intensity = new InputSlot<float>();

        [Input(Guid = "ff3442c5-95c8-4bd6-a492-cb4a9a597ea1")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "81962c9b-2fcd-432a-b2e7-c31b743ecd02")]
        public readonly InputSlot<float> Range = new InputSlot<float>();

        [Input(Guid = "f3ca8d13-4e24-4718-a59c-6a1b9a2a3c04")]
        public readonly InputSlot<bool> IsEnabled = new InputSlot<bool>();
        
        [Input(Guid = "B5EE1E4B-3C8C-48DF-BBCF-AAC614DE6EC9")]
        public readonly InputSlot<T3.Core.Operator.GizmoVisibility> ShowGizmo = new InputSlot<T3.Core.Operator.GizmoVisibility>();
        
        [Input(Guid = "3babb43d-afe6-4c34-a4c6-950d1e3971cc")]
        public readonly InputSlot<float> GizmoSize = new InputSlot<float>();

        [Input(Guid = "d6f6838c-4b36-41a8-86c5-1b1fe5dcece1")]
        public readonly InputSlot<float> Decay = new InputSlot<float>();

    }
}

