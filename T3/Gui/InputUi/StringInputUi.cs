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
        private const int MAX_STRING_LENGTH = 255;

        public enum UsageType
        {
            Default,
            FilePath,
            DirectoryPath,
        }

        public UsageType Usage { get; set; } = UsageType.Default;

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
                case UsageType.FilePath:
                case UsageType.DirectoryPath:
                    inputEditStateFlags = DrawEditWithSelectors(ref value);
                    break;
            }

            inputEditStateFlags |= ImGui.IsItemClicked() ? InputEditStateFlags.Started : InputEditStateFlags.Nothing;
            inputEditStateFlags |= ImGui.IsItemDeactivatedAfterEdit() ? InputEditStateFlags.Finished : InputEditStateFlags.Nothing;

            return inputEditStateFlags;
        }

        private InputEditStateFlags DrawEditWithSelectors(ref string value)
        {
            ImGui.SetNextItemWidth(-70);
            InputEditStateFlags inputEditStateFlags = DrawDefaultTextEdit(ref value);
            ImGui.SameLine();
            if (ImGui.Button("...", new Vector2(30, 0)))
            {
                string newPath = Usage == UsageType.FilePath ? FileOperations.PickResourceFilePath() : FileOperations.PickResourceDirectory();
                if (!string.IsNullOrEmpty(newPath))
                {
                    value = newPath;
                    inputEditStateFlags = InputEditStateFlags.Modified | InputEditStateFlags.Finished;
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
                    try
                    {
                        Process.Start(value);
                    }
                    catch (Win32Exception e)
                    {
                        Log.Warning("Can't open editor: " + e.Message);
                    }
                }
            }

            return inputEditStateFlags;
        }

        private static InputEditStateFlags DrawDefaultTextEdit(ref string value)
        {
            bool changed = ImGui.InputText("##textEdit", ref value, MAX_STRING_LENGTH);
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