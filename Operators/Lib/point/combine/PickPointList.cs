using T3.Core.Utils;

namespace lib.point.combine;

[Guid("e9a0bdfd-6f6e-41e2-8c0c-cd5fee26e359")]
public class PickPointList : Instance<PickPointList>
{
    [Output(Guid = "205F066B-8214-437A-9D7C-8665F6E8E979")]
    public readonly Slot<BufferWithViews> Selected = new();

    public PickPointList()
    {
        Selected.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        var connections = Input.GetCollectedTypedInputs();
        if (connections == null || connections.Count == 0)
            return;

        var index = Index.GetValue(context).Mod(connections.Count);
        Selected.Value = connections[index].GetValue(context);
    }

    [Input(Guid = "C00F5AF4-EFB6-4A9D-B0AD-D8DBC69EED23")]
    public readonly MultiInputSlot<BufferWithViews> Input = new();

    [Input(Guid = "8B418DCD-E7CD-4BCE-A69E-A9A543426BFE")]
    public readonly InputSlot<int> Index = new(0);
}