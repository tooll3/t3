namespace Lib.point.modify;

[Guid("695d20dc-d1fe-4648-80fb-e1159b8aead4")]
internal sealed class SelectPointsWithField : Instance<SelectPointsWithField>
,ITransformable
{
    [Output(Guid = "7a77a26e-88b5-43ed-84b1-b35682ceda7d")]
    public readonly TransformCallbackSlot<BufferWithViews> Result2 = new();

    public SelectPointsWithField()
    {
        Result2.TransformableOp = this;
    }

    IInputSlot ITransformable.TranslationInput => VolumeCenter;
    IInputSlot ITransformable.RotationInput => VolumeRotate;
    IInputSlot ITransformable.ScaleInput => VolumeScale;
    public Action<Instance, EvaluationContext> TransformCallback { get; set; }

        [Input(Guid = "af41409a-81ce-43c0-8cf7-1787e3b47b70")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "f38834a9-1bfe-4edc-9b70-2d0c57967556")]
        public readonly InputSlot<float> Strength = new InputSlot<float>();

        [Input(Guid = "74bc9dbf-015e-4182-95ef-cfca4be09f32", MappedType = typeof(FModes))]
        public readonly InputSlot<int> StrengthFactor = new InputSlot<int>();

        [Input(Guid = "b0b20881-ccb2-46e6-b936-db892ac04202", MappedType = typeof(FModes))]
        public readonly InputSlot<int> WriteTo = new InputSlot<int>();

        [Input(Guid = "07177f13-ef76-4e81-91b5-49cf8eb73cbd", MappedType = typeof(Modes))]
        public readonly InputSlot<int> Mode = new InputSlot<int>();

        [Input(Guid = "03558959-4190-4b5b-a544-b17d56fb77da")]
        public readonly InputSlot<bool> ClampResult = new InputSlot<bool>();

        [Input(Guid = "80dc1a9c-2452-4d82-bb19-a68de1c5a104", MappedType = typeof(Shapes))]
        public readonly InputSlot<int> VolumeShape = new InputSlot<int>();

        [Input(Guid = "8c9e71d9-6271-44eb-a09d-b04271e69d26")]
        public readonly InputSlot<System.Numerics.Vector3> VolumeCenter = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "b3bb960b-f378-4316-8b91-3216b3b8b5cd")]
        public readonly InputSlot<System.Numerics.Vector3> VolumeStretch = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "27353540-c561-4e11-9b7f-e60a55b2e85d")]
        public readonly InputSlot<float> VolumeScale = new InputSlot<float>();

        [Input(Guid = "b1f637bb-0ad4-47d1-a933-1b4385bc79e8")]
        public readonly InputSlot<System.Numerics.Vector3> VolumeRotate = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "d41776c7-74ef-45fd-b507-ff5a3b6c0125")]
        public readonly InputSlot<float> FallOff = new InputSlot<float>();

        [Input(Guid = "c9fbe013-7050-4714-a43d-68960d6afbea")]
        public readonly InputSlot<System.Numerics.Vector2> BiasAndGain = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "61acc5cc-f084-4c88-8d54-207645dc841f")]
        public readonly InputSlot<float> Scatter = new InputSlot<float>();

        [Input(Guid = "3973e7d6-8d1a-4725-802c-42ce2f5eeaad")]
        public readonly InputSlot<float> Phase = new InputSlot<float>();

        [Input(Guid = "004caed6-9acf-414e-b1f5-c23201d3ef89")]
        public readonly InputSlot<float> Threshold = new InputSlot<float>();

        [Input(Guid = "89277a1c-bb76-4dfc-9672-def1ff5ce7e3")]
        public readonly InputSlot<T3.Core.Operator.GizmoVisibility> Visibility = new InputSlot<T3.Core.Operator.GizmoVisibility>();

        [Input(Guid = "bc002ec2-cd53-4cf3-800c-868f2cacbb0c")]
        public readonly InputSlot<bool> DiscardNonSelected = new InputSlot<bool>();

        [Input(Guid = "13ec688a-cbee-4999-a434-b36c64d764f1")]
        public readonly InputSlot<bool> SetW = new InputSlot<bool>();

        [Input(Guid = "3da5b21f-f31b-4f11-9f1c-0deee0efac40")]
        public readonly InputSlot<T3.Core.DataTypes.FieldShaderDefinition> Field = new InputSlot<T3.Core.DataTypes.FieldShaderDefinition>();


        
    private enum Shapes
    {
        Sphere,
        Box,
        Plane,
        Zebra,
        Noise,
    }
        
    private enum Modes
    {
        Override,
        Add,
        Sub,
        Multiply,
        Invert,
    }
    
    private enum FModes
    {
        None,
        F1,
        F2,
    }
}