using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Selection;
using T3.Gui.TypeColors;
using Buffer = SharpDX.Direct3D11.Buffer;
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
        ShowOptions = 0x8,
        ModifiedAndFinished = Modified | Finished
    }

    public enum Relevancy
    {
        Required,
        Relevant,
        Optional
    }

    public interface IInputUi : ISelectable
    {
        Symbol.InputDefinition InputDefinition { get; set; }
        Guid Id { get; }
        Type Type { get; }
        Relevancy Relevancy { get; set; }

        InputEditState DrawInputEdit(IInputSlot input, SymbolUi compositionUi, SymbolChildUi symbolChildUi);

        void DrawParameterEdits();

        void Write(JsonTextWriter writer);
        void Read(JToken inputToken);
    }

    public abstract class InputValueUi<T> : IInputUi
    {
        public static float ConnectionAreaWidth = 30;
        public static float ParameterNameWidth = 120;

        public Symbol.InputDefinition InputDefinition { get; set; }
        public Guid Id => InputDefinition.Id;
        public Relevancy Relevancy { get; set; } = Relevancy.Required;
        protected abstract InputEditState DrawEditControl(string name, ref T value);
        protected abstract void DrawValueDisplay(string name, ref T value);

        public InputEditState DrawInputEdit(IInputSlot inputSlot, SymbolUi compositionUi, SymbolChildUi symbolChildUi)
        {
            var name = inputSlot.Input.Name;
            var editState = InputEditState.Nothing;
            var typeColor = TypeUiRegistry.Entries[Type].Color;
            var animator = compositionUi.Symbol.Animator;
            bool animated = animator.IsInputSlotAnimated(inputSlot);

            if (inputSlot is InputSlot<T> typedInputSlot)
            {
                var input = inputSlot.Input;
                if (inputSlot.IsConnected || animated)
                {
                    if (typedInputSlot.IsMultiInput)
                    {
                        // Just show actual value
                        ImGui.Button(name + "##paramName", new Vector2(-1, 0));
                        if (ImGui.BeginPopupContextItem("##parameterOptions", 0))
                        {
                            if (ImGui.MenuItem("Parameters settings"))
                                editState = InputEditState.ShowOptions;

                            ImGui.EndPopup();
                        }

                        var multiInput = (MultiInputSlot<T>)typedInputSlot;
                        var allInputs = multiInput.GetCollectedTypedInputs();

                        for (int multiInputIndex = 0; multiInputIndex < allInputs.Count; multiInputIndex++)
                        {
                            ImGui.PushID(multiInputIndex);

                            ImGui.PushStyleColor(ImGuiCol.Button, ColorVariations.Highlight.Apply(typeColor).Rgba);
                            if (ImGui.Button("->", new Vector2(ConnectionAreaWidth, 0)))
                            {
                                symbolChildUi.IsSelected = false;
                                var compositionSymbol = compositionUi.Symbol;
                                var allConnections = compositionSymbol.Connections.FindAll(c => c.TargetParentOrChildId == symbolChildUi.Id && c.TargetSlotId == inputSlot.Id);
                                var connection = allConnections[multiInputIndex];
                                var sourceUi = compositionUi.GetSelectables()
                                                            .First(ui => ui.Id == connection.SourceParentOrChildId || ui.Id == connection.SourceSlotId);
                                sourceUi.IsSelected = true;
                            }
                            ImGui.PopStyleColor();

                            ImGui.SameLine();

                            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(1f, 0.5f));
                            ImGui.Button("#" + multiInputIndex, new Vector2(ParameterNameWidth, 0));
                            ImGui.PopStyleVar();
                            ImGui.SameLine();

                            ImGui.SetNextItemWidth(-1);
                            ImGui.PushStyleColor(ImGuiCol.Text, T3Style.ConnectedParameterColor.Rgba);
                            var slot = allInputs[multiInputIndex];
                            DrawValueDisplay("##multiInputParam", ref slot.Value);
                            ImGui.PopStyleColor();
                            ImGui.PopID();
                        }
                    }
                    else
                    {
                        if (animated)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Button, ColorVariations.Highlight.Apply(typeColor).Rgba);
                            if (ImGui.Button("A", new Vector2(ConnectionAreaWidth, 0)))
                            {
                                animator.RemoveAnimationFrom(inputSlot);
                            }
                        }
                        else
                        {
                            ImGui.PushStyleColor(ImGuiCol.Button, ColorVariations.Highlight.Apply(typeColor).Rgba);
                            if (ImGui.Button("->", new Vector2(ConnectionAreaWidth, 0)))
                            {
                                symbolChildUi.IsSelected = false;
                                var compositionSymbol = compositionUi.Symbol;
                                var con = compositionSymbol.Connections.First(c => c.TargetParentOrChildId == symbolChildUi.Id && c.TargetSlotId == inputSlot.Id);
                                var sourceUi = compositionUi.GetSelectables()
                                                            .First(ui => ui.Id == con.SourceParentOrChildId || ui.Id == con.SourceSlotId);
                                sourceUi.IsSelected = true;
                            }
                        }

                        ImGui.PopStyleColor();
                        ImGui.SameLine();

                        // Draw Name
                        ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(1f, 0.5f));
                        ImGui.Button(input.Name + "##ParamName", new Vector2(ParameterNameWidth, 0));
                        if (ImGui.BeginPopupContextItem("##parameterOptions", 0))
                        {
                            if (ImGui.MenuItem("Parameters settings"))
                                editState = InputEditState.ShowOptions;

                            ImGui.EndPopup();
                        }

                        ImGui.PopStyleVar();

                        ImGui.SameLine();

                        // Draw control
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

                        ImGui.SetNextItemWidth(-1);

                        DrawValueDisplay(name, ref typedInputSlot.Value);
                        ImGui.PopStyleColor();
                        ImGui.PopItemWidth();
                    }
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, ColorVariations.Operator.Apply(typeColor).Rgba);
                    if (ImGui.Button("", new Vector2(ConnectionAreaWidth, 0)))
                    {
                        animator.CreateInputUpdateAction<float>(inputSlot);
                    }

                    ImGui.PopStyleColor();
                    ImGui.SameLine();

                    // Draw Name
                    ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(1f, 0.5f));
                    ImGui.Button(input.Name + "##ParamName", new Vector2(ParameterNameWidth, 0));
                    if (ImGui.BeginPopupContextItem("##parameterOptions", 0))
                    {
                        if (ImGui.MenuItem("Set as default", !input.IsDefault))
                            input.SetCurrentValueAsDefault();

                        if (ImGui.MenuItem("Reset to default", !input.IsDefault))
                            input.ResetToDefault();

                        if (ImGui.MenuItem("Parameters settings"))
                            editState = InputEditState.ShowOptions;

                        ImGui.EndPopup();
                    }

                    ImGui.PopStyleVar();

                    ImGui.SameLine();

                    // Draw control
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

                    ImGui.SetNextItemWidth(-1);
                    editState |= DrawEditControl(name, ref typedInputSlot.TypedInputValue.Value);

                    if ((editState & InputEditState.Focused) == InputEditState.Focused)
                    {
                        Log.Debug($"focused {name}");
                    }

                    if ((editState & InputEditState.Modified) == InputEditState.Modified)
                    {
                        inputSlot.DirtyFlag.Invalidate();
                        Log.Debug($"modified {typedInputSlot.TypedInputValue.Value}");
                    }

                    if ((editState & InputEditState.Finished) == InputEditState.Finished)
                    {
                        Log.Debug($"Edit {name} completed with {typedInputSlot.TypedInputValue.Value}");
                    }

                    input.IsDefault &= (editState & InputEditState.Modified) != InputEditState.Modified;

                    ImGui.PopStyleColor();
                    ImGui.PopItemWidth();
                }
            }
            else
            {
                Debug.Assert(false);
            }

            return editState;
        }

        public virtual void DrawParameterEdits()
        {
            Type enumType = typeof(Relevancy);
            var values = Enum.GetValues(enumType);
            var valueNames = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                valueNames[i] = Enum.GetName(typeof(Relevancy), values.GetValue(i));
            }

            int index = (int)Relevancy;
            ImGui.Combo("##dropDownRelevancy", ref index, valueNames, valueNames.Length);
            Relevancy = (Relevancy)index;
            ImGui.SameLine();
            ImGui.Text("Relevancy");
        }

        public virtual void Write(JsonTextWriter writer)
        {
            writer.WriteObject("Relevancy", Relevancy.ToString());
            writer.WritePropertyName("Position");
            writer.WriteStartObject();
            writer.WriteValue("X", PosOnCanvas.X);
            writer.WriteValue("Y", PosOnCanvas.Y);
            writer.WriteEndObject();
        }

        public virtual void Read(JToken inputToken)
        {
            Relevancy = (Relevancy)Enum.Parse(typeof(Relevancy), inputToken["Relevancy"].ToString());
            JToken positionToken = inputToken["Position"];
            PosOnCanvas = new Vector2(positionToken["X"].Value<float>(), positionToken["Y"].Value<float>());
        }

        public Type Type { get; } = typeof(T);
        public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = new Vector2(100, 30);
        public bool IsSelected { get; set; }
    }

    public abstract class SingleControlInputUi<T> : InputValueUi<T>
    {
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

    public class FloatListInputUi : SingleControlInputUi<List<float>>
    {
        public override bool DrawSingleEditControl(string name, ref List<float> list)
        {
            var outputString = string.Join(", ", list);
            ImGui.Text($"{outputString}");
            return false;
        }

        protected override void DrawValueDisplay(string name, ref List<float> list)
        {
            var outputString = string.Join(", ", list);
            ImGui.Text($"{outputString}");
        }
    }

    public class IntInputUi : SingleControlInputUi<int>
    {
        public override bool DrawSingleEditControl(string name, ref int value)
        {
            return ImGui.DragInt("##intParam", ref value);
        }

        protected override void DrawValueDisplay(string name, ref int value)
        {
            ImGui.InputInt(name, ref value, 0, 0, ImGuiInputTextFlags.ReadOnly);
        }
    }

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

    public class Size2InputUi : SingleControlInputUi<Size2>
    {
        public override bool DrawSingleEditControl(string name, ref Size2 value)
        {
            return ImGui.DragInt2("##int2Edit", ref value.Width);
        }

        protected override void DrawValueDisplay(string name, ref Size2 value)
        {
            DrawEditControl(name, ref value);
        }
    }

    public class Texture2dInputUi : SingleControlInputUi<Texture2D>
    {
        public override bool DrawSingleEditControl(string name, ref Texture2D value)
        {
            return false;//ImGui.DragInt2("##int2Edit", ref value.Width);
        }

        protected override void DrawValueDisplay(string name, ref Texture2D value)
        {
//            DrawEditControl(name, ref value);
        }
    }

    public class ComputeShaderInputUi : SingleControlInputUi<ComputeShader>
    {
        public override bool DrawSingleEditControl(string name, ref ComputeShader value)
        {
            return false;//ImGui.DragInt2("##int2Edit", ref value.Width);
        }

        protected override void DrawValueDisplay(string name, ref ComputeShader value)
        {
//            DrawEditControl(name, ref value);
        }
    }

    public class BufferInputUi : SingleControlInputUi<Buffer>
    {
        public override bool DrawSingleEditControl(string name, ref Buffer value)
        {
            return false;//ImGui.DragInt2("##int2Edit", ref value.Width);
        }

        protected override void DrawValueDisplay(string name, ref Buffer value)
        {
//            DrawEditControl(name, ref value);
        }
    }

    public class EnumInputUi<T> : InputValueUi<T> where T : Enum
    {
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
                if (ImGui.TreeNode("##enumParam"))
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
                bool modified = ImGui.Combo("##dropDownParam", ref index, valueNames, valueNames.Length);
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
        public static Dictionary<Type, Func<IInputUi>> Entries { get; } = new Dictionary<Type, Func<IInputUi>>();
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