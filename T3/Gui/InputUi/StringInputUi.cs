using System;
using System.Numerics;
using System.Windows.Forms;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;

namespace T3.Gui.InputUi
{
    public class StringInputUi : SingleControlInputUi<string>
    {
        private const int MAX_STRING_LENGTH = 255;

        public enum UsageType
        {
            Default,
            Path,
        }

        public UsageType Usage { get; set; } = UsageType.Default;

        public override bool DrawSingleEditControl(string name, ref string value)
        {
            if (value != null)
            {
                switch (Usage)
                {
                    case UsageType.Default:
                        return ImGui.InputText("##textEdit", ref value, MAX_STRING_LENGTH);
                    case UsageType.Path:
                    {
                        ImGui.SetNextItemWidth(-30);
                        bool changed = ImGui.InputText("##textEditPath", ref value, MAX_STRING_LENGTH);
                        ImGui.SameLine();
                        if (ImGui.Button("...", new Vector2(30, 0)))
                        {
                            using (OpenFileDialog openFileDialog = new OpenFileDialog())
                            {
                                openFileDialog.InitialDirectory = "c:\\";
                                openFileDialog.Filter = "jpg files (*.jpg)|*.jpg";
                                openFileDialog.FilterIndex = 2;
                                openFileDialog.RestoreDirectory = true;

                                if (openFileDialog.ShowDialog() == DialogResult.OK)
                                {
                                    value = openFileDialog.FileName;
                                    changed = true;
                                }
                            }
                        }

                        return changed;
                    }
                }
            }

            // value was null!
            ImGui.Text(name + " is null?!");
            return false;
        }

        protected override void DrawValueDisplay(string name, ref string value)
        {
            if (value != null)
            {
                ImGui.InputText(name, ref value, MAX_STRING_LENGTH, ImGuiInputTextFlags.ReadOnly);
            }
            else
            {
                string nullString = "<null>";
                ImGui.InputText(name, ref nullString, MAX_STRING_LENGTH, ImGuiInputTextFlags.ReadOnly);
            }
        }

        public override void DrawParameterEdits()
        {
            base.DrawParameterEdits();

            Type enumType = typeof(UsageType);
            var values = Enum.GetValues(enumType);
            var valueNames = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                valueNames[i] = Enum.GetName(typeof(UsageType), values.GetValue(i));
            }

            int index = (int)Usage;
            ImGui.Combo("##dropDownStringUsage", ref index, valueNames, valueNames.Length);
            Usage = (UsageType)index;
            ImGui.SameLine();
            ImGui.Text("Usage");
        }

        public override void Write(JsonTextWriter writer)
        {
            base.Write(writer);

            writer.WriteObject("Usage", Usage.ToString());
        }

        public override void Read(JToken inputToken)
        {
            base.Read(inputToken);

            Usage = (UsageType)Enum.Parse(typeof(UsageType), inputToken["Usage"].Value<string>());
        }
    }
}