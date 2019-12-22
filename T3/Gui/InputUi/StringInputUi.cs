using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Numerics;
using System.Windows.Forms;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;
using T3.Core.Logging;
using T3.Gui.UiHelpers;

namespace T3.Gui.InputUi
{
    public class StringInputUi : InputValueUi<string>
    {
        private const int MAX_STRING_LENGTH = 255;

        public enum UsageType
        {
            Default,
            FilePath,
            DirectoryPath,
        }

        public UsageType Usage { get; set; } = UsageType.Default;

        protected override InputEditState DrawEditControl(string name, ref string value)
        {
            if (value == null)
            {
                // value was null!
                ImGui.Text(name + " is null?!");
                return InputEditState.Nothing;
            }

            InputEditState inputEditState = InputEditState.Nothing;
            switch (Usage)
            {
                case UsageType.Default:
                    inputEditState = DrawDefaultTextEdit(ref value);
                    break;
                case UsageType.FilePath:
                case UsageType.DirectoryPath:
                    inputEditState = DrawEditWithSelectors(ref value);
                    break;
            }

            inputEditState |= ImGui.IsItemClicked() ? InputEditState.Started : InputEditState.Nothing;
            inputEditState |= ImGui.IsItemDeactivatedAfterEdit() ? InputEditState.Finished : InputEditState.Nothing;

            return inputEditState;
        }

        private InputEditState DrawEditWithSelectors(ref string value)
        {
            ImGui.SetNextItemWidth(-70);
            InputEditState inputEditState = DrawDefaultTextEdit(ref value);
            ImGui.SameLine();
            if (ImGui.Button("...", new Vector2(30, 0)))
            {
                string newPath = Usage == UsageType.FilePath ? FileOperations.PickResourceFilePath() : FileOperations.PickResourceDirectory();
                if (!string.IsNullOrEmpty(newPath))
                {
                    value = newPath;
                    inputEditState = InputEditState.Modified | InputEditState.Finished;
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("Edit", new Vector2(40, 0)))
            {
                if (!File.Exists(value))
                {
                    Log.Error("Can't open non-existing file " + value);
                }
                else
                {
                    Process.Start(value);
                }
            }

            return inputEditState;
        }

        private static InputEditState DrawDefaultTextEdit(ref string value)
        {
            bool changed = ImGui.InputText("##textEdit", ref value, MAX_STRING_LENGTH);
            return changed ? InputEditState.Modified : InputEditState.Nothing;
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

        public override void DrawSettings()
        {
            base.DrawSettings();

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