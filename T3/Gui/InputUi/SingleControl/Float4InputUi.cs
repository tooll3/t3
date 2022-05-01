using System.Numerics;
using ImGuiNET;
using T3.Gui.Interaction;

namespace T3.Gui.InputUi.SingleControl
{
    public class Float4InputUi : FloatVectorInputValueUi<Vector4>
    {
        public Float4InputUi() : base(4)
        {
            Min = 0;
            Max = 1;
            Clamp = true;
        }
        
        public override IInputUi Clone()
        {
            return CloneWithType<Float4InputUi>();
        }
        
        protected override InputEditStateFlags DrawEditControl(string name, ref Vector4 float4Value)
        {
            float4Value.CopyTo(FloatComponents);
            var thumbWidth = ImGui.GetFrameHeight();
            var inputEditState = VectorValueEdit.Draw(FloatComponents, Min, Max, Scale, Clamp, thumbWidth);

            ImGui.SameLine();
            float4Value = new Vector4(FloatComponents[0], 
                                      FloatComponents[1],
                                      FloatComponents[2],
                                      FloatComponents[3]);
            
            if (ColorEditButton.Draw(ref float4Value, Vector2.Zero))
            {
                float4Value.CopyTo(FloatComponents);
                inputEditState |= InputEditStateFlags.Modified;
            }
            return inputEditState;
        }
    }
}