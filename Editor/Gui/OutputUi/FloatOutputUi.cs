using System.Diagnostics;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.OutputUi;

public class FloatOutputUi : OutputUi<float>
{
    public override IOutputUi Clone()
    {
        return new FloatOutputUi()
                   {
                       OutputDefinition = OutputDefinition,
                       PosOnCanvas = PosOnCanvas,
                       Size = Size
                   };
        // TODO: check if curve should be cloned too
    }
        
    protected override void DrawTypedValue(ISlot slot)
    {
        if (slot is Slot<float> typedSlot)
        {
            var value = typedSlot.Value;
                
            if (slot != _lastSlot)
            {
                _lastSlot = slot;
                _curve.Reset(value);
            }

            _curve.Draw(value);
        }
        else
        {
            Debug.Assert(false);
        }
    }
        
    private ISlot _lastSlot;
    private readonly CurvePlotCanvas _curve = new(resolution: 500);
    private readonly VectorCurvePlotCanvas<float> _curve2 = new(resolution: 500);
}