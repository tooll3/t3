using ImGuiNET;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Selection;
using Vector2 = System.Numerics.Vector2;

namespace T3.Gui
{
    [Flags]
    public enum InputEditState
    {
        Nothing = 0x0,
        Focused = 0x1,
        Modified = 0x2,
        Finished = 0x4,
        ModifiedAndFinished = Modified | Finished
    }

    public interface IInputUi : ISelectable
    {
        Type Type { get; }

        InputEditState DrawInputEdit(IInputSlot input, Instance op, SymbolChildUi symbolChildUi);
    }

    public abstract class InputValueUi<T> : IInputUi
    {
        protected InputValueUi(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; }
        protected abstract InputEditState DrawEditControl(string name, ref T value);
        protected abstract void DrawValueDisplay(string name, ref T value);





        public InputEditState DrawInputEdit(IInputSlot inputSlot, Instance op, SymbolChildUi symbolChildUi)
        {
            DrawConnectionArea(inputSlot, op, symbolChildUi);

            var name = inputSlot.Input.Name;
            if (inputSlot is InputSlot<T> typedInputSlot)
            {
                if (inputSlot.IsConnected)
                {
                    if (typedInputSlot.IsMultiInput)
                    {
                        // Just show actual value
                        ImGui.PushItemWidth(200.0f);
                        ImGui.PushStyleColor(ImGuiCol.Text, Color.Red.Rgba);
                        var multiInput = (MultiInputSlot<T>)typedInputSlot;
                        var allInputs = multiInput.GetCollectedInputs();
                        foreach (var input in allInputs)
                        {
                            DrawValueDisplay(name, ref input.Value);
                        }

                        ImGui.PopStyleColor();
                        ImGui.PopItemWidth();
                    }
                    else
                    {
                        // Just show actual value
                        ImGui.PushItemWidth(200.0f);
                        ImGui.PushStyleColor(ImGuiCol.Text, Color.Red.Rgba);
                        DrawValueDisplay(name, ref typedInputSlot.Value);
                        ImGui.PopStyleColor();
                        ImGui.PopItemWidth();
                    }
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

                    var editState = DrawEditControl(name, ref typedInputSlot.TypedInputValue.Value);

                    if (editState.HasFlag(InputEditState.Focused))
                    {
                        Log.Debug($"focused {name}");
                    }

                    if (editState.HasFlag(InputEditState.Modified))
                    {
                        Log.Debug($"modified {typedInputSlot.TypedInputValue.Value}");
                    }

                    if (editState.HasFlag(InputEditState.Finished))
                    {
                        Log.Debug($"Edit {name} completed with {typedInputSlot.TypedInputValue.Value}");
                    }

                    input.IsDefault &= !editState.HasFlag(InputEditState.Modified);

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

                    return editState;
                }
            }
            else
            {
                Debug.Assert(false);
            }

            return InputEditState.Nothing;
        }


        private void DrawConnectionArea(IInputSlot inputSlot, Instance op, SymbolChildUi symbolChildUi)
        {
            ImGui.SetNextItemWidth(50);

            ImGui.PushStyleColor(ImGuiCol.Button,
                IsSelected
                ? Color.White.Rgba
                : Color.Gray.Rgba);
            ImGui.Button("", new Vector2(5, 0));
            ImGui.PopStyleColor();

            ImGui.SameLine();

            if (inputSlot.IsConnected)
            {
                if (ImGui.Button("->", new Vector2(50, 0)))
                {
                    symbolChildUi.IsSelected = false;
                    var c = op.Parent.Symbol.Connections.FirstOrDefault(c2 => c2.TargetParentOrChildId == op.Id && c2.TargetSlotId == inputSlot.Id);
                    var targetOpId = c.SourceParentOrChildId;
                    var foundChild = op.Parent.Children.FirstOrDefault(child => child.Id == targetOpId);
                    if (foundChild != null)
                    {
                        var parentUi = SymbolUiRegistry.Entries[op.Parent.Symbol.Id];
                        var sourceChildUi = parentUi.ChildUis.FirstOrDefault(xx => xx.Id == targetOpId);
                        sourceChildUi.IsSelected = true;
                    }
                    // ToDo Do something in canvas
                }
            }
            else if (ImGui.Button("", new Vector2(50, 0)))
            {
                //open context menu
            }
            ImGui.SameLine(0);

        }


        public Type Type { get; } = typeof(T);
        public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = new Vector2(100, 30);
        public bool IsSelected { get; set; }
    }

    public abstract class SingleControlInputUi<T> : InputValueUi<T>
    {
        protected SingleControlInputUi(Guid id) : base(id)
        {
        }

        public abstract bool DrawSingleEditControl(string name, ref T value);

        protected override InputEditState DrawEditControl(string name, ref T value)
        {
            bool valueModified = DrawSingleEditControl(name, ref value);

            InputEditState inputEditState = InputEditState.Nothing;
            inputEditState |= ImGui.IsItemClicked() ? InputEditState.Focused : InputEditState.Nothing;
            inputEditState |= valueModified ? InputEditState.Modified : InputEditState.Nothing;
            inputEditState |= ImGui.IsItemDeactivatedAfterEdit() ? InputEditState.Finished : InputEditState.Nothing;

            return inputEditState;
        }
    }

    public class FloatInputUi : SingleControlInputUi<float>
    {
        public FloatInputUi(Guid id) : base(id)
        {
        }

        public override bool DrawSingleEditControl(string name, ref float value)
        {
            return ImGui.DragFloat(name, ref value);
        }

        protected override void DrawValueDisplay(string name, ref float value)
        {
            ImGui.InputFloat(name, ref value, 0.0f, 0.0f, "%f", ImGuiInputTextFlags.ReadOnly);
        }
    }

    public class IntInputUi : SingleControlInputUi<int>
    {
        public IntInputUi(Guid id) : base(id)
        {
        }

        public override bool DrawSingleEditControl(string name, ref int value)
        {
            return ImGui.DragInt(name, ref value);
        }

        protected override void DrawValueDisplay(string name, ref int value)
        {
            ImGui.InputInt(name, ref value, 0, 0, ImGuiInputTextFlags.ReadOnly);
        }
    }

    public class StringInputUi : SingleControlInputUi<string>
    {
        private const int MAX_STRING_LENGTH = 255;

        public StringInputUi(Guid id) : base(id)
        {
        }

        public override bool DrawSingleEditControl(string name, ref string value)
        {
            if (value != null)
            {
                return ImGui.InputText(name, ref value, MAX_STRING_LENGTH);
            }
            else
            {
                ImGui.Text(name + " is null?!");
                return false;
            }
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
    }

    public class Size2InputUi : SingleControlInputUi<Size2>
    {
        public Size2InputUi(Guid id) : base(id)
        {
        }

        public override bool DrawSingleEditControl(string name, ref Size2 value)
        {
            return ImGui.DragInt2(name, ref value.Width);
        }

        protected override void DrawValueDisplay(string name, ref Size2 value)
        {
            DrawEditControl(name, ref value);
        }
    }

    public class EnumInputUi<T> : InputValueUi<T> where T : Enum
    {
        public EnumInputUi(Guid id) : base(id)
        {
        }

        protected override InputEditState DrawEditControl(string name, ref T value)
        {
            // todo: check perf impact of creating the list here again and again! -> cache lists
            Type enumType = typeof(T);
            var values = Enum.GetValues(enumType);
            var valueNames = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                valueNames[i] = Enum.GetName(typeof(T), values.GetValue(i));
            }

            if (enumType.GetCustomAttributes<FlagsAttribute>().Any())
            {
                // show as checkboxes
                InputEditState editState = InputEditState.Nothing;
                if (ImGui.TreeNode(name))
                {
                    // todo: refactor crappy code below, works but ugly!
                    bool[] checks = new bool[values.Length];
                    int intValue = (int)(object)value;
                    for (int i = 0; i < valueNames.Length; i++)
                    {
                        int enumValueAsInt = (int)values.GetValue(i);
                        checks[i] = (intValue & enumValueAsInt) > 0;
                        if (ImGui.Checkbox(valueNames[i], ref checks[i]))
                        {
                            // value modified, store new flag
                            if (checks[i])
                            {
                                intValue |= enumValueAsInt;
                            }
                            else
                            {
                                intValue &= ~enumValueAsInt;
                            }

                            value = (T)(object)intValue;
                            editState |= InputEditState.Modified;
                        }

                        if (ImGui.IsItemClicked())
                        {
                            editState |= InputEditState.Focused;
                        }

                        if (ImGui.IsItemDeactivatedAfterEdit())
                        {
                            editState |= InputEditState.Finished;
                        }
                    }

                    ImGui.TreePop();
                }

                return editState;
            }
            else
            {
                int index = (int)(object)value;
                InputEditState editState = InputEditState.Nothing;
                bool modified = ImGui.Combo(name, ref index, valueNames, valueNames.Length);
                if (modified)
                {
                    value = (T)values.GetValue(index);
                    editState |= InputEditState.ModifiedAndFinished;
                }

                if (ImGui.IsItemClicked())
                {
                    editState |= InputEditState.Focused;
                }

                return editState;
            }
        }

        protected override void DrawValueDisplay(string name, ref T value)
        {
            ImGui.Text(value.ToString());
        }
    }

    public static class InputUiFactory
    {
        public static Dictionary<Type, Func<Guid, IInputUi>> Entries { get; } = new Dictionary<Type, Func<Guid, IInputUi>>();
    }

    public interface ITypeUiProperties
    {
        Color Color { get; set; }
    }

    public class FloatUiProperties : ITypeUiProperties
    {
        public Color Color { get; set; } = TypeUiRegistry.ColorForValues;
    }

    public class StringUiProperties : ITypeUiProperties
    {
        public Color Color { get; set; } = TypeUiRegistry.ColorForString;
    }

    public class Size2UiProperties : ITypeUiProperties
    {
        public Color Color { get; set; } = TypeUiRegistry.ColorForValues;
    }

    public class IntUiProperties : ITypeUiProperties
    {
        public Color Color { get; set; } = TypeUiRegistry.ColorForValues;
    }

    public class TextureUiProperties : ITypeUiProperties
    {
        public Color Color { get; set; } = TypeUiRegistry.ColorForTextures;
    }

    /// <summary>
    /// Internal implementation things that are below the tech level of normal artists.
    /// </summary>
    public class ShaderUiProperties : ITypeUiProperties
    {
        public Color Color { get; set; } = new Color(0.518f, 0.046f, 0.228f, 1.000f);
    }

    public class FallBackUiProperties : ITypeUiProperties
    {
        public Color Color { get; set; } = new Color(0.518f, 0.046f, 0.228f, 1.000f);
    }

    public static class TypeUiRegistry
    {
        public static Dictionary<Type, ITypeUiProperties> Entries { get; } = new Dictionary<Type, ITypeUiProperties>();

        public static ITypeUiProperties GetPropertiesForType(Type type)
        {
            var t = FallBackTypeUiProperties;
            if (type != null)
                Entries.TryGetValue(type, out t);
            return t;
        }

        public static ITypeUiProperties FallBackTypeUiProperties = new FallBackUiProperties();

        internal static Color ColorForValues = new Color(0.525f, 0.550f, 0.554f, 1.000f);
        internal static Color ColorForString = new Color(0.468f, 0.586f, 0.320f, 1.000f);
        internal static Color ColorForTextures = new Color(0.803f, 0.313f, 0.785f, 1.000f);
    }
}