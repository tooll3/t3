namespace Examples.t3;

[Guid("966b11dd-c9e2-4352-a249-6ca1ac5ad030")]
public class MovingShapes : Instance<MovingShapes>, ITransformable
{
    public MovingShapes()
    {
        Output.TransformableOp = this;
    }
        
    [Output(Guid = "bc52a8fa-c610-443a-b13b-23ef43aed003")]
    public readonly TransformCallbackSlot<Command> Output = new();

    [Input(Guid = "8350af8e-cb0c-438b-9881-b4d18ee71e77")]
    public readonly InputSlot<System.Numerics.Vector3> Translation = new();

    [Input(Guid = "1cbebd99-14cb-4824-9604-a45ed97d8714", MappedType = typeof(Shapes))]
    public readonly InputSlot<int> Shape = new();

    [Input(Guid = "7c6609fc-5c27-4392-92df-492438bd23b0")]
    public readonly InputSlot<float> UniformScale = new();

        
    private enum Shapes
    {
        Cube,
        Sphere,
    }

    public IInputSlot TranslationInput => Translation;
    public IInputSlot RotationInput { get; }
    public IInputSlot ScaleInput { get; }
    public Action<Instance, EvaluationContext> TransformCallback { get; set; }
}