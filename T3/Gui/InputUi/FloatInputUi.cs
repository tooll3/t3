using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;

namespace T3.Gui.InputUi
{
    public class FloatInputUi : SingleControlInputUi<float>
    {
        public float Min = -100.0f;
        public float Max = 100.0f;

        public override bool DrawSingleEditControl(string name, ref float value)
        {
            return ImGui.DragFloat("##floatEdit", ref value, 0.0f, Min, Max);
        }

        protected override void DrawValueDisplay(string name, ref float value)
        {
            ImGui.InputFloat(name, ref value, 0.0f, 0.0f, "%f", ImGuiInputTextFlags.ReadOnly);
        }

        public override void DrawParameterEdits()
        {
            base.DrawParameterEdits();

            ImGui.DragFloat("Min", ref Min);
            ImGui.DragFloat("Max", ref Max);
        }

        public override void Write(JsonTextWriter writer)
        {
            base.Write(writer);

            writer.WriteValue("Min", Min);
            writer.WriteValue("Max", Max);
        }

        public override void Read(JToken inputToken)
        {
            base.Read(inputToken);

            Min = inputToken["Min"].Value<float>();
            Max = inputToken["Max"].Value<float>();
        }
    }
}