using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Interaction;

namespace T3.Gui.InputUi.SingleControl
{
    public class IntInputUi : SingleControlInputUi<int>
    {
        public override bool IsAnimatable => true;

        public override IInputUi Clone()
        {
            return new IntInputUi()
                   {
                       InputDefinition = InputDefinition,
                       Parent = Parent,
                       PosOnCanvas = PosOnCanvas,
                       Relevancy = Relevancy
                   };
        }

        protected override bool DrawSingleEditControl(string name, ref int value)
        {
            var result= SingleValueEdit.Draw(ref value, new Vector2(-1, 0));
            return result == InputEditStateFlags.Modified;
        }

        public  InputEditStateFlags DrawEditControl(ref int value)
        {
            return SingleValueEdit.Draw(ref value, -Vector2.UnitX, -100, 100, false, 0);
        }
        
        
        protected override void DrawAnimatedValue(string name, InputSlot<int> inputSlot, Animator animator)
        {
            double time = EvaluationContext.GlobalTimeInBars;
            var curves = animator.GetCurvesForInput(inputSlot);
            foreach (var curve in curves)
            {
                int value = (int)curve.GetSampledValue(time);
                var editState = DrawEditControl(name, ref value);
                if ((editState & InputEditStateFlags.Modified) == InputEditStateFlags.Modified)
                {
                    // Animated ints are constant by default
                    var key = new VDefinition()
                                        {
                                            InType = VDefinition.Interpolation.Constant,
                                            OutType = VDefinition.Interpolation.Constant,
                                            InEditMode = VDefinition.EditMode.Constant,
                                            OutEditMode = VDefinition.EditMode.Constant,                                            
                                            Value = value,
                                        };
                    
                    curve.AddOrUpdateV(time, key);
                }
            }
        }

        
        protected override string GetSlotValueAsString(ref int value)
        {
            // This is a stub of value editing. Sadly it's very hard to get
            // under control because of styling issues and because in GraphNodes
            // The op body captures the mouse event first.
            //SingleValueEdit.Draw(ref floatValue,  -Vector2.UnitX);
            
            return value.ToString();
        }
        
        protected override void DrawReadOnlyControl(string name, ref int value)
        {
            ImGui.InputInt(name, ref value, 0, 0, ImGuiInputTextFlags.ReadOnly);
        }
    }
}