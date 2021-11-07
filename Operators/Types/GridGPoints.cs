using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3ee8f66d_68df_43c1_b0eb_407259bf7e86
{
    public class GridGPoints : Instance<GridGPoints>, ITransformable
    {

        [Output(Guid = "eb8c79d4-d147-419c-a606-4bbe7b71933f")]
        public readonly TransformCallbackSlot<T3.Core.DataTypes.BufferWithViews> OutBuffer = new TransformCallbackSlot<T3.Core.DataTypes.BufferWithViews>();

        
        public GridGPoints()
        {
            OutBuffer.TransformableOp = this;
        }
        
        System.Numerics.Vector3 ITransformable.Translation { get => Center.Value; set => Center.SetTypedInputValue(value); }
        System.Numerics.Vector3 ITransformable.Rotation { get => System.Numerics.Vector3.Zero; set { } }
        System.Numerics.Vector3 ITransformable.Scale { get => System.Numerics.Vector3.One; set { } }

        public Action<ITransformable, EvaluationContext> TransformCallback { get => OutBuffer.TransformCallback; set => OutBuffer.TransformCallback = value; }

        [Input(Guid = "72eda38f-fc49-4b1f-b7c0-97e07bee4f7c")]
        public readonly InputSlot<int> CountX = new InputSlot<int>();

        [Input(Guid = "8c46fc72-8960-4247-a5ef-dd38f822f1bb")]
        public readonly InputSlot<int> CountY = new InputSlot<int>();

        [Input(Guid = "6de4f08a-5834-4b9b-93e8-8f93fe32164c")]
        public readonly InputSlot<int> CountZ = new InputSlot<int>();

        [Input(Guid = "37a11e3d-e353-4b0f-a052-356582e235b0")]
        public readonly InputSlot<System.Numerics.Vector3> Size = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "940133dd-4e45-4a78-8b13-8831e30f78b8")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();

        [Input(Guid = "0f053c34-c9ef-46b7-9c73-fff9984a3d5e")]
        public readonly InputSlot<System.Numerics.Vector3> Center = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "70459c2d-8686-4709-9a12-1ea36d1b08d2")]
        public readonly InputSlot<float> W = new InputSlot<float>();

        [Input(Guid = "e2019c63-f498-4ccb-a2cc-ea2ade0c540b")]
        public readonly InputSlot<System.Numerics.Vector3> OrientationAxis = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "28f5fea3-b7c1-4e46-84d5-47b5f311be80")]
        public readonly InputSlot<float> OrientationAngle = new InputSlot<float>();

        [Input(Guid = "d910b40e-6bee-4e1a-82a9-625b89fc27eb")]
        public readonly InputSlot<System.Numerics.Vector3> Pivot = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "9748853e-5f13-45c9-bca6-d07b07185aab", MappedType = typeof(SizeModes))]
        public readonly InputSlot<int> SizeMode = new InputSlot<int>();

        private enum SizeModes
        {
            Cell,
            Bounds,
        }
    }
}

