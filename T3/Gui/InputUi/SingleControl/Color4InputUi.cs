using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Interaction;

namespace T3.Gui.InputUi.SingleControl
{
    public class Vector4InputUi : InputValueUi<Vector4>
    {
        public override bool IsAnimatable => true;

        public override IInputUi Clone()
        {
            return new Vector4InputUi()
                   {
                       InputDefinition = InputDefinition,
                       Parent = Parent,
                       PosOnCanvas = PosOnCanvas,
                       Relevancy = Relevancy
                   };
        }

        protected override InputEditStateFlags DrawEditControl(string name, ref Vector4 float4Value)
        {
            float4Value.CopyTo(_components);
            var thumbWidth = ImGui.GetFrameHeight();
            var width = (ImGui.GetContentRegionAvail().X - thumbWidth) / 4f -1;
            var size = new Vector2(width, 0);

            var resultingEditState = InputEditStateFlags.Nothing;
            for (var index = 0; index < 4; index++)
            {
                if (index > 0)
                    ImGui.SameLine();

                ImGui.PushID(index);
                resultingEditState |= SingleValueEdit.Draw(ref _components[index], size: size, 0, 1, true, 0.01f);
                ImGui.PopID();
            }
            
            float4Value = new Vector4(_components[0], _components[1], _components[2], _components[3]);
            
            ImGui.SameLine();
            
            
            if (ColorEditButton.Draw(ref float4Value, Vector2.Zero))
            {
                resultingEditState |= InputEditStateFlags.Modified;
            }
            return resultingEditState;
        }
        
        
        

        private static float[] _components = new float[4];    // static to avoid GC allocations
        

        protected override void DrawReadOnlyControl(string name, ref Vector4 value)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Color.Blue.Rgba);
            DrawEditControl(name, ref value);
            ImGui.PopStyleColor();
        }
        
        protected override void DrawAnimatedValue(string name, InputSlot<Vector4> inputSlot, Animator animator)
        {
            double time = EvaluationContext.GlobalTimeInBars;
            var curves = animator.GetCurvesForInput(inputSlot).ToArray();
            if (curves.Length < 4)
            {
                DrawReadOnlyControl(name, ref inputSlot.Value);
                return;
            }

            SharpDX.Vector4 value = new SharpDX.Vector4((float)curves[0].GetSampledValue(time),
                                                        (float)curves[1].GetSampledValue(time),
                                                        (float)curves[2].GetSampledValue(time),
                                                        (float)curves[3].GetSampledValue(time));
            Vector4 editValue = new Vector4(value.X, value.Y, value.Z, value.W);
            
            var inputEditState = DrawEditControl(name, ref editValue);
            if (inputEditState == InputEditStateFlags.Nothing)
                return;
            
            SharpDX.Vector4 newValue = new SharpDX.Vector4(editValue.X, editValue.Y, editValue.Z, editValue.W);
            for (int i = 0; i < 4; i++)
            {
                if (Math.Abs(newValue[i] - value[i]) > float.Epsilon)
                {
                    var key = curves[i].GetV(time) ?? new VDefinition() { U = time };
                    key.Value = newValue[i];
                    curves[i].AddOrUpdateV(time, key);
                }
            }
        }
    }
}