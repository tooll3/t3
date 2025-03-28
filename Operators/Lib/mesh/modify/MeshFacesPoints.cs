namespace Lib.mesh.modify;

[Guid("58a490b1-eb8a-4102-906a-f74a79c0ad1c")]
internal sealed class MeshFacesPoints : Instance<MeshFacesPoints>
{

    [Output(Guid = "f40d8abc-a97c-4dba-8811-b19042db1c66")]
    public readonly Slot<BufferWithViews> OutBuffer = new Slot<BufferWithViews>();

    [Input(Guid = "bad1db42-201c-4d3b-8e62-82e812a8388f")]
    public readonly InputSlot<MeshBuffers> InputMesh = new InputSlot<MeshBuffers>();

    [Input(Guid = "9361aeff-d96f-400c-8837-7d98fb6da99e")]
    public readonly InputSlot<float> W = new InputSlot<float>();

    [Input(Guid = "67b24f6d-61d1-49df-9e86-4555474f91b4")]
    public readonly InputSlot<Vector3> OffsetByTBN = new InputSlot<Vector3>();

    [Input(Guid = "13184872-063b-473a-bf3d-68cd1356a261")]
    public readonly InputSlot<float> StretchZ = new InputSlot<float>();

    [Input(Guid = "ffe056e2-c937-4d91-a2ad-73e7988ea994")]
    public readonly InputSlot<Vector4> Color = new InputSlot<Vector4>();
        
        
    private enum Directions
    {
        Surface,
        Noise,
        Center,
    }
        

}