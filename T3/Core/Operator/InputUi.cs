using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using T3.Core.Operator;
using T3.Gui.Selection;

namespace T3.Gui
{
    public interface IInputUi : ISelectable
    {
        void DrawInputEdit(string name, IInputSlot input);
        Color Color { get; }
    }

    public abstract class InputValueUi<T> : IInputUi
    {
        public abstract bool DrawEditControl(string name, ref T value);
        public abstract void DrawValueDisplay(string name, ref T value);

        public void DrawInputEdit(string name, IInputSlot inputSlot)
        {
            if (inputSlot is InputSlot<T> typedInputSlot)
            {
                if (inputSlot.IsConnected)
                {
                    // just show actual value
                    ImGui.PushItemWidth(200.0f);
                    ImGui.PushStyleColor(ImGuiCol.Text, Color.TRed.Rgba);
                    DrawValueDisplay(name, ref typedInputSlot.Value);
                    ImGui.PopStyleColor();
                    ImGui.PopItemWidth();
                }
                else
                {
                    var input = inputSlot.Input;

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

                    bool valueModified = DrawEditControl(name, ref typedInputSlot.TypedInputValue.Value);
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
            }
            else
            {
                Debug.Assert(false);
            }

        }

        public virtual Color Color { get; } = Color.TGreen;
        public Vector2 Position { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = new Vector2(100, 30);
        public bool IsSelected { get; set; }
    }

    public class FloatInputUi : InputValueUi<float>
    {
        public override bool DrawEditControl(string name, ref float value)
        {
            return ImGui.DragFloat(name, ref value);
        }

        public override void DrawValueDisplay(string name, ref float value)
        {
            ImGui.InputFloat(name, ref value, 0.0f, 0.0f, "%f", ImGuiInputTextFlags.ReadOnly);
        }

        public override Color Color { get; } = Color.Gray;
    }

    public class IntInputUi : InputValueUi<int>
    {
        public override bool DrawEditControl(string name, ref int value)
        {
            return ImGui.DragInt(name, ref value);
        }

        public override void DrawValueDisplay(string name, ref int value)
        {
            ImGui.InputInt(name, ref value, 0, 0, ImGuiInputTextFlags.ReadOnly);
        }

        public override Color Color { get; } = Color.TBlue;
    }

    public class StringInputUi : InputValueUi<string>
    {
        private const int MAX_STRING_LENGTH = 255;

        public override bool DrawEditControl(string name, ref string value)
        {
            return ImGui.InputText(name, ref value, MAX_STRING_LENGTH);
        }

        public override void DrawValueDisplay(string name, ref string value)
        {
            DrawEditControl(name, ref value);
        }
    }

    public static class InputUiRegistry
    {
        /// <summary>
        /// Provides a dictionary of <see cref="Symbol.InputDefinition.id"/> -> <see cref="IInputUi"/>s for a <see cref="Symbol"/>
        /// </summary>
        public static Dictionary<Guid, Dictionary<Guid, IInputUi>> Entries { get; } = new Dictionary<Guid, Dictionary<Guid, IInputUi>>();

        public static Dictionary<Type, IInputUi> EntriesByType { get; } = new Dictionary<Type, IInputUi>();
    }
}
