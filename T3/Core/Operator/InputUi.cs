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
        Color Color { get; }
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
                if (input.IsDefault)
                {
                    // handling default values is a bit tricky with ImGui as we want to show the default
                    // value when this is set, but we never want the default value to be modified. But as
                    // editing is already done when the return value of the ImGui edit control tells us
                    // that editing has happened this here is a simple way to ensure that the default value
                    // is always correct but editing is only happening on the input value.
                    input.Value.Assign(input.DefaultValue);
                }
                bool valueModified = DrawEditControl(name, ref typedValue.Value);
                input.IsDefault &= !valueModified;
                ImGui.PopStyleColor();
                ImGui.PopItemWidth();

//                 T3.Logging.Log.Info($"Color is {Color}");

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

        public virtual Color Color { get; } = Color.TGreen;
    }

    public class FloatInputUi : InputValueUi<float>
    {
        public override bool DrawEditControl(string name, ref float value)
        {
            return ImGui.DragFloat(name, ref value);
        }

        public override Color Color { get; } = Color.Gray;
    }

    public class IntInputUi : InputValueUi<int>
    {
        public override bool DrawEditControl(string name, ref int value)
        {
            return ImGui.DragInt(name, ref value);
        }

        public override Color Color { get; } = Color.TBlue;
    }

    public static class InputUiRegistry
    {
        public static Dictionary<Type, IInputUi> Entries { get; } = new Dictionary<Type, IInputUi>();
    }
}
