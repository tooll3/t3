namespace Lib.point.generate;

[Guid("3ee8f66d-68df-43c1-b0eb-407259bf7e86")]
public class GridPoints : Instance<GridPoints>
                         ,ITransformable
{

    [Output(Guid = "eb8c79d4-d147-419c-a606-4bbe7b71933f")]
    public readonly TransformCallbackSlot<BufferWithViews> OutBuffer = new();

        
    public GridPoints()
    {
        OutBuffer.TransformableOp = this;
    }

    IInputSlot ITransformable.TranslationInput => Center;
    IInputSlot ITransformable.RotationInput => null;
    IInputSlot ITransformable.ScaleInput => null;

    public Action<Instance, EvaluationContext> TransformCallback { get; set; }
        
    [Input(Guid = "72eda38f-fc49-4b1f-b7c0-97e07bee4f7c")]
    public readonly InputSlot<int> CountX = new();

    [Input(Guid = "8c46fc72-8960-4247-a5ef-dd38f822f1bb")]
    public readonly InputSlot<int> CountY = new();

    [Input(Guid = "6de4f08a-5834-4b9b-93e8-8f93fe32164c")]
    public readonly InputSlot<int> CountZ = new();

    [Input(Guid = "37a11e3d-e353-4b0f-a052-356582e235b0")]
    public readonly InputSlot<Vector3> Size = new();

    [Input(Guid = "940133dd-4e45-4a78-8b13-8831e30f78b8")]
    public readonly InputSlot<float> Scale = new();

    [Input(Guid = "0f053c34-c9ef-46b7-9c73-fff9984a3d5e")]
    public readonly InputSlot<Vector3> Center = new();

    [Input(Guid = "70459c2d-8686-4709-9a12-1ea36d1b08d2")]
    public readonly InputSlot<float> W = new();

    [Input(Guid = "e2019c63-f498-4ccb-a2cc-ea2ade0c540b")]
    public readonly InputSlot<Vector3> OrientationAxis = new();

    [Input(Guid = "28f5fea3-b7c1-4e46-84d5-47b5f311be80")]
    public readonly InputSlot<float> OrientationAngle = new();

    [Input(Guid = "d910b40e-6bee-4e1a-82a9-625b89fc27eb")]
    public readonly InputSlot<Vector3> Pivot = new();

    [Input(Guid = "9748853e-5f13-45c9-bca6-d07b07185aab", MappedType = typeof(SizeModes))]
    public readonly InputSlot<int> SizeMode = new();

    [Input(Guid = "4686AA6E-C8E0-45EE-B9C3-1CC7294F305E", MappedType = typeof(Tilings))]
    public readonly InputSlot<int> Tiling = new();

    [Input(Guid = "aa19b556-660b-4267-be44-30c30d85314e")]
    public readonly InputSlot<Vector4> Color = new InputSlot<Vector4>();

    private enum SizeModes
    {
        Cell,
        Bounds,
    }
        
    private enum Tilings
    {
        Cartesian,
        Triangular,
        HoneyCombs,
        Diagonal,
    }
}