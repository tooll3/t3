using ImGuiNET;
using System.Numerics;

namespace T3.Gui.InputUi
{
    public class Vector2InputUi : SingleControlInputUi<Vector2>
    {
        public override bool IsAnimatable => false;

        public override bool DrawSingleEditControl(string name, ref Vector2 value)
        {
            return ImGui.DragFloat2("##Vector2Edit", ref value);
        }

        protected override void DrawValueDisplay(string name, ref Vector2 value)
        {
            DrawEditControl(name, ref value);
        }
        
        // protected override void DrawAnimatedValue(string name, InputSlot<Vector4> inputSlot, Animator animator)
        // {
        //     double time = EvaluationContext.GlobalTime;
        //     var curves = animator.GetCurvesForInput(inputSlot).ToArray();
        //     if (curves.Length < 4)
        //     {
        //         DrawValueDisplay(name, ref inputSlot.Value);
        //         return;
        //     }
        //
        //     SharpDX.Vector4 value = new SharpDX.Vector4((float)curves[0].GetSampledValue(time),
        //                                                 (float)curves[1].GetSampledValue(time),
        //                                                 (float)curves[2].GetSampledValue(time),
        //                                                 (float)curves[3].GetSampledValue(time));
        //     Vector4 editValue = new Vector4(value.X, value.Y, value.Z, value.W);
        //     var edited = DrawSingleEditControl(name, ref editValue);
        //     if (!edited)
        //         return; // nothing changed
        //
        //     SharpDX.Vector4 newValue = new SharpDX.Vector4(editValue.X, editValue.Y, editValue.Z, editValue.W);
        //     for (int i = 0; i < 4; i++)
        //     {
        //         if (Math.Abs(newValue[i] - value[i]) > Single.Epsilon)
        //         {
        //             var key = curves[i].GetV(time);
        //             if (key == null)
        //                 key = new VDefinition() { U = time };
        //             key.Value = newValue[i];
        //             curves[i].AddOrUpdateV(time, key);
        //         }
        //     }
        // }
    }
}