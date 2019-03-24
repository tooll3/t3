using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using ImGuiNET;

namespace T3.Core.Operator
{
    /// <summary>
    /// Properties needed for visual representation of an instance. Should later be moved to gui component.
    /// </summary>
    public class InstanceUi
    {
        public Instance Instance;
        public Vector2 Position = Vector2.Zero;
        public Vector2 Size = new Vector2(100, 30);
        public bool Visible = true;
        public bool Selected = false;
        public string Name { get; set; } = string.Empty;
        public string ReadableName => string.IsNullOrEmpty(Name) ? Instance.Symbol.SymbolName : Name;
    }

    public interface InputUi
    {
        void DrawInputEdit(Slot inputValue);
    }

    public class FloatInputUi : InputUi
    {
        public void DrawInputEdit(Slot inputSlot)
        {
            if (inputSlot is InputSlot<float> floatSlot)
            {
                ImGui.DragFloat(floatSlot.Name, ref floatSlot.TypedInputValue.Value);
                //ImGui.InputFloat("bla", ref floatValue.Value);
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }

    public class IntInputUi : InputUi
    {
        public void DrawInputEdit(Slot inputSlot)
        {
            if (inputSlot is InputSlot<int> intSlot)
            {
                ImGui.DragInt(intSlot.Name, ref intSlot.TypedInputValue.Value);
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }

    public static class InputUiRegistry
    {
        public static Dictionary<Type, InputUi> Entries { get; } = new Dictionary<Type, InputUi>();
    }

}