using System;
using System.Linq;
using ImGuiNET;
using System.Numerics;
using System.Runtime.Remoting.Messaging;
using T3.Core.Animation;
using T3.Core.Operator;

namespace T3.Gui.InputUi
{
    public class Vector4InputUi : SingleControlInputUi<Vector4>
    {
        public override bool IsAnimatable => true;

        public override bool DrawSingleEditControl(string name, ref Vector4 value)
        {
            return ImGui.ColorEdit4("##Vector4Edit", ref value, ImGuiColorEditFlags.Float);
        }

        protected override void DrawValueDisplay(string name, ref Vector4 value)
        {
            DrawEditControl(name, ref value);
        }
        
        protected override void DrawAnimatedValue(string name, InputSlot<Vector4> inputSlot, Animator animator)
        {
            double time = EvaluationContext.GlobalTime;
            var curves = animator.GetCurvesForInput(inputSlot).ToArray();
            if (curves.Length < 4)
            {
                DrawValueDisplay(name, ref inputSlot.Value);
                return;
            }

            SharpDX.Vector4 value = new SharpDX.Vector4((float)curves[0].GetSampledValue(time),
                                                        (float)curves[1].GetSampledValue(time),
                                                        (float)curves[2].GetSampledValue(time),
                                                        (float)curves[3].GetSampledValue(time));
            Vector4 editValue = new Vector4(value.X, value.Y, value.Z, value.W);
            var edited = DrawSingleEditControl(name, ref editValue);
            if (!edited)
                return; // nothing changed

            SharpDX.Vector4 newValue = new SharpDX.Vector4(editValue.X, editValue.Y, editValue.Z, editValue.W);
            for (int i = 0; i < 4; i++)
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