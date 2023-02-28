using System;
using System.Numerics;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Resource;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.InputUi.SimpleInputUis
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

        public UsageType Usage { get; private set; } = UsageType.Default;
        public string FileFilter { get; private set; } = null;

        public override IInputUi Clone()
        {
            return new StringInputUi
                       {
                           InputDefinition = InputDefinition,
                           Parent = Parent,
                           PosOnCanvas = PosOnCanvas,
                           Relevancy = Relevancy,
                           Size = Size,
                           Usage = Usage,
                           FileFilter = FileFilter,
                       };
        }

        protected override InputEditStateFlags DrawEditControl(string name, ref string value)
        {
            if (value == null)
            {
                // value was null!
                ImGui.TextUnformatted(name + " is null?!");
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
                    inputEditStateFlags = DrawEditWithSelectors(FileOperations.FilePickerTypes.File, ref value, FileFilter);
                    break;
                case UsageType.DirectoryPath:
                    inputEditStateFlags = DrawEditWithSelectors(FileOperations.FilePickerTypes.Folder, ref value);
                    break;
            }

            inputEditStateFlags |= ImGui.IsItemClicked() ? InputEditStateFlags.Started : InputEditStateFlags.Nothing;
            inputEditStateFlags |= ImGui.IsItemDeactivatedAfterEdit() ? InputEditStateFlags.Finished : InputEditStateFlags.Nothing;

            return inputEditStateFlags;
        }

        private static InputEditStateFlags DrawEditWithSelectors(FileOperations.FilePickerTypes type, ref string value, string filter = null)
        {
            ImGui.SetNextItemWidth(-70);
            var inputEditStateFlags = DrawDefaultTextEdit(ref value);

            if (ImGui.IsItemHovered() && ImGui.CalcTextSize(value).X > ImGui.GetItemRectSize().X)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.One * 5);
                ImGui.BeginTooltip();
                ImGui.TextUnformatted(value);
                ImGui.EndTooltip();
                ImGui.PopStyleVar();
            }

            ImGui.SameLine();
            var modifiedByPicker = FileOperations.DrawFileSelector(type, ref value, filter);
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
            ImGui.Dummy(new Vector2(1, 1));
            var changed = ImGui.InputTextMultiline("##textEdit", ref value, MAX_STRING_LENGTH, new Vector2(-1, 150));
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
            FormInputs.AddVerticalSpace();

            {
                var tmpForRef = Usage;
                if (FormInputs.AddEnumDropdown(ref tmpForRef, "Usage"))
                    Usage = tmpForRef;

            }
            
            {
                var tmp = FileFilter;
                var warning = !string.IsNullOrEmpty(tmp) && !tmp.Contains('|')
                                  ? "Filter must include at least one | symbol.\nPlease read tooltip for examples"
                                  : null;

                if (FormInputs.AddStringInput("File Filter", ref tmp, null, warning,
                                              "This will only work for file FilePath-Mode.\nThe filter has to be in following format:\n\n Your Description (*.ext)|*.ext"))
                {
                    FileFilter = tmp;
                }
            }
        }

        public override void Write(JsonTextWriter writer)
        {
            base.Write(writer);

            writer.WriteObject("Usage", Usage.ToString());
            writer.WriteObject("Filter", FileFilter);
        }

        public override void Read(JToken inputToken)
        {
            base.Read(inputToken);

            Usage = (UsageType)Enum.Parse(typeof(UsageType), inputToken["Usage"].Value<string>());
        }
    }
}