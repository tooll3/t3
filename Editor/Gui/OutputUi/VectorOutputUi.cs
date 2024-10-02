using System.Diagnostics;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.OutputUi
{
    public class VectorOutputUi<T> : OutputUi<T>
    {
        public override IOutputUi Clone()
        {
            return new VectorOutputUi<T>()
                       {
                           OutputDefinition = OutputDefinition,
                           PosOnCanvas = PosOnCanvas,
                           Size = Size
                       };
            // TODO: check if curve should be cloned too
        }

        protected override void DrawTypedValue(ISlot slot)
        {
            if (slot is Slot<T> typedSlot)
            {
                var value = typedSlot.Value;

                if (slot != _lastSlot)
                {
                    _lastSlot = slot;
                    _curve2.Reset(default);
                }

                _curve2.Draw(value);
            }
            else
            {
                Debug.Assert(false);
            }
        }

        private ISlot _lastSlot;

        private readonly VectorCurvePlotCanvas<T> _curve2 = new(resolution: 500);
    }
}