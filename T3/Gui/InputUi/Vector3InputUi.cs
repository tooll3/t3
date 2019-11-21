using System;
using System.Linq;
using ImGuiNET;
using System.Numerics;
using T3.Core.Animation;
using T3.Core.Operator;

namespace T3.Gui.InputUi
{
    public class Vector3InputUi : SingleControlInputUi<Vector3>
    {
        public override bool IsAnimatable => true;

        public override bool DrawSingleEditControl(string name, ref Vector3 value)
        {
            return ImGui.DragFloat3("##Vector3Edit", ref value);
        }

        protected override void DrawValueDisplay(string name, ref Vector3 value)
        {
            DrawEditControl(name, ref value);
        }
        
        protected override void DrawAnimatedValue(string name, InputSlot<Vector3> inputSlot, Animator animator)
        {
            double time = EvaluationContext.GlobalTime;
            var curves = animator.GetCurvesForInput(inputSlot).ToArray();
            if (curves.Length < 3)
            {
                DrawValueDisplay(name, ref inputSlot.Value);
                return;
            }

            SharpDX.Vector3 value = new SharpDX.Vector3((float)curves[0].GetSampledValue(time),
                                                        (float)curves[1].GetSampledValue(time),
                                                        (float)curves[2].GetSampledValue(time));
            Vector3 editValue = new Vector3(value.X, value.Y, value.Z);
            var edited = DrawSingleEditControl(name, ref editValue);
            if (!edited)
                return; // nothing changed

            SharpDX.Vector3 newValue = new SharpDX.Vector3(editValue.X, editValue.Y, editValue.Z);
            for (int i = 0; i < 3; i++)
            {
                if (Math.Abs(newValue[i] - value[i]) > Single.Epsilon)
                {
                    var key = curves[i].GetV(time);
                    if (key == null)
                        key = new VDefinition() { U = time };
                    key.Value = newValue[i];
                    curves[i].AddOrUpdateV(time, key);
                }
            }
        }
    }
}