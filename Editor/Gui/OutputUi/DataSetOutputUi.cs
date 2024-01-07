using T3.Core.DataTypes.DataSet;
using T3.Core.Operator.Slots;

namespace T3.Editor.Gui.OutputUi;

public class DataSetOutputUi : OutputUi<float>
{
    public override IOutputUi Clone()
    {
        return new DataSetOutputUi()
                   {
                       OutputDefinition = OutputDefinition,
                       PosOnCanvas = PosOnCanvas,
                       Size = Size
                   };
    }


    protected override void DrawTypedValue(ISlot slot)
    {
        if (slot is not Slot<DataSet> dataSetSlot)
            return;

        //DrawDataSet(dataSetSlot.Value);
        _canvas.Draw(dataSetSlot.Value);
    }

    private readonly DataSetViewCanvas _canvas = new();
}