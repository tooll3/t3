using System;
using System.ComponentModel;
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
        private const int MAX_STRING_LENGTH = 4000;

        public enum UsageType
        {
            Default,
            FilePath,
            DirectoryPath,
            Multiline,
        }

        public UsageType Usage { get; set; } = UsageType.Default;

        public override IInputUi Clone()
        {
            return new StringInputUi
                   {
                       InputDefinition = InputDefinition,
                       Parent = Parent,
                       PosOnCanvas = PosOnCanvas,
                       Relevancy = Relevancy,
                       Size = Size,
                       Usage = Usage
                   };
        }

        protected override InputEditStateFlags DrawEditControl(string name, ref string value)
        {
            if (value == null)
            {
                // value was null!
                ImGui.Text(name + " is null?!");
                return InputEditStateFlags.Nothing;
            }

            InputEditStateFlags inputEditStateFlags = InputEditStateFlags.Nothing;
            switch (Usage)
            {
                case UsageType.Default:
                    inputEditStateFlags = DrawDefaultTextEdit(ref value);
                    break;
                case UsageType.Multiline:
                    inputEditStateFlags = DrawMultilineTextEdit(ref value);
                    break;
                case UsageType.FilePath:
                    inputEditStateFlags = DrawEditWithSelectors(FileOperations.FilePickerTypes.File, ref value);
                    break;
                case UsageType.DirectoryPath:
                    inputEditStateFlags = DrawEditWithSelectors(FileOperations.FilePickerTypes.Folder, ref value);
                    break;
            }

            inputEditStateFlags |= ImGui.IsItemClicked() ? InputEditStateFlags.Started : InputEditStateFlags.Nothing;
            inputEditStateFlags |= ImGui.IsItemDeactivatedAfterEdit() ? InputEditStateFlags.Finished : InputEditStateFlags.Nothing;

            return inputEditStateFlags;
        }


        private static InputEditStateFlags DrawEditWithSelectors(FileOperations.FilePickerTypes type, ref string value)
        {
            ImGui.SetNextItemWidth(-70);
            var inputEditStateFlags = DrawDefaultTextEdit(ref value);
            ImGui.SameLine();
            var modifiedByPicker = FileOperations.DrawSelector(type, ref value);
            if (modifiedByPicker)
            {
                inputEditStateFlags = InputEditStateFlags.Modified | InputEditStateFlags.Finished;
            }

            return inputEditStateFlags;
        }

        private static InputEditStateFlags DrawDefaultTextEdit(ref string value)
        {
            bool changed = ImGui.InputText("##textEdit", ref value, MAX_STRING_LENGTH);
            return changed ? InputEditStateFlags.Modified : InputEditStateFlags.Nothing;
        }

        private static InputEditStateFlags DrawMultilineTextEdit(ref string value)
        {
            ImGui.Dummy(new Vector2(1,1));
            var changed = ImGui.InputTextMultiline("##textEdit", ref value, MAX_STRING_LENGTH, new Vector2(-1, 300));
            return changed ? InputEditStateFlags.Modified : InputEditStateFlags.Nothing;
        }

        
        protected override void DrawReadOnlyControl(string name, ref string value)
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