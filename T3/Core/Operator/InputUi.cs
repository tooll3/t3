using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using T3.Core.Operator;

namespace T3.Gui
{
    public interface IInputUi
    {
        void DrawInputEdit(string name, SymbolChild.Input input);
    }

    public abstract class InputValueUi<T> : IInputUi where T : struct
    {
        public abstract bool DrawEditControl(string name, ref T value);

        public void DrawInputEdit(string name, SymbolChild.Input input)
        {
            if (input.Value is InputValue<T> typedValue)
            {
                // draw control
                ImGui.PushItemWidth(200.0f);
                ImGui.PushStyleColor(ImGuiCol.Text, input.IsDefault ? Color.Gray.Rgba : Color.White.Rgba);
                bool valueModified = DrawEditControl(name, ref typedValue.Value);
                input.IsDefault &= !valueModified;
                ImGui.PopStyleColor();
                ImGui.PopItemWidth();

                // draw reset button
                ImGui.SameLine(200.0f, 130.0f);
                if (ImGui.Button("Reset To Default"))
                {
                    input.ResetToDefault();
                }

                // draw set as default button
                ImGui.SameLine(330.0f, 130.0f);
                if (ImGui.Button("Set As Default"))
                {
                    input.SetCurrentValueAsDefault();
                }
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }

    public class FloatInputUi : InputValueUi<float>
    {
        public override bool DrawEditControl(string name, ref float value)
        {
            return ImGui.DragFloat(name, ref value);
        }
    }

    public class IntInputUi : InputValueUi<int>
    {
        public override bool DrawEditControl(string name, ref int value)
        {
            return ImGui.DragInt(name, ref value);
        }
    }

    public static class InputUiRegistry
    {
        public static Dictionary<Type, IInputUi> Entries { get; } = new Dictionary<Type, IInputUi>();
    }
}
