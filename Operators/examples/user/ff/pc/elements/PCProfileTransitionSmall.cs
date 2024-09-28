namespace Examples.user.ff.pc.elements;

[Guid("ecaad404-88a8-42e2-a46c-9358b7552550")]
public class PCProfileTransitionSmall : Instance<PCProfileTransitionSmall>
{
    [Output(Guid = "f7a454dc-88c1-4c8c-9cad-0fc5fd833142")]
    public readonly Slot<Command> Output = new();

    [Input(Guid = "efd55c50-32e8-44f7-b7ae-9eb97b14e788")]
    public readonly InputSlot<string> ProfileImage = new();

    [Input(Guid = "1555e7d0-277d-4997-9000-560fe2733002")]
    public readonly InputSlot<string> BrandImage = new();

    [Input(Guid = "7c4333f1-659a-4262-89a1-d47f6ecb385f")]
    public readonly InputSlot<float> Transition = new();

    [Input(Guid = "d86bd5cd-e793-4f9d-be4c-f2957ef1c2ac")]
    public readonly InputSlot<float> Scale = new();

    [Input(Guid = "03200903-c977-4611-8b30-1ebdac9abba0")]
    public readonly InputSlot<System.Numerics.Vector3> Offset = new();


}