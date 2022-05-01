using System.Numerics;
using T3.Gui.Interaction;

namespace T3.Gui.InputUi
{
    public class FloatInputUi : FloatVectorInputValueUi<float>
    {
        public FloatInputUi() : base(1) { }
        
        public override IInputUi Clone()
        {
            return CloneWithType<FloatInputUi>();
        }
        
        protected override InputEditStateFlags DrawEditControl(string name, ref float value)
        {
            FloatComponents[0] = value;
            var inputEditState = VectorValueEdit.Draw(FloatComponents, Min, Max, Scale, Clamp);
            value = FloatComponents[0];
            return inputEditState;
        }
        
        public InputEditStateFlags DrawEditControl(ref float value)
        {
            return SingleValueEdit.Draw(ref value, -Vector2.UnitX, Min, Max, Clamp, Scale);
        }
    }
}