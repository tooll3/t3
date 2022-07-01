using System.Linq;
using ImGuiNET;
using SharpDX;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Slots;

namespace T3.Gui.InputUi.SingleControl
{
    public class Int3InputUi : SingleControlInputUi<Int3>
    {
        public override IInputUi Clone()
        {
            return new Int3InputUi()
                   {
                       InputDefinition = InputDefinition,
                       Parent = Parent,
                       PosOnCanvas = PosOnCanvas,
                       Relevancy = Relevancy
                   };
        }

        protected override bool DrawSingleEditControl(string name, ref Int3 value)
        {
            return ImGui.DragInt3("##int3Edit", ref value.X);
        }

        protected override void DrawReadOnlyControl(string name, ref Int3 value)
        {
            DrawEditControl(name, ref value);
        }
        
        public override void ApplyValueToAnimation(IInputSlot inputSlot, InputValue inputValue, Animator animator, double time) 
        {
            if (inputValue is InputValue<Int3> float3InputValue)
            {
                Int3 value = float3InputValue.Value;
                var curves = animator.GetCurvesForInput(inputSlot).ToArray();
                Curve.UpdateCurveValues(curves, time, new [] { (float)value.X, (float)value.Y, (float)value.Z});   
            }
        }
    }
}