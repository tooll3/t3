using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using T3.Gui.Selection;

namespace T3.Core.Operator
{
    /// <summary>
    /// Properties needed for visual representation of an instance. Should later be moved to gui component.
    /// </summary>
    public class SymbolChildUi : ISelectable
    {
        public SymbolChild SymbolChild;
        public Vector2 Position { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = new Vector2(100, 30);
        public bool IsVisible { get; set; } = true;
        public bool IsSelected { get; set; } = false;
        public string Name { get; set; } = string.Empty;
        public string ReadableName => string.IsNullOrEmpty(Name) ? SymbolChild.Symbol.SymbolName : Name;
    }

    public interface IInputUi
    {
        bool DrawInputEdit(string name, InputValue inputValue);
    }

    public class FloatInputUi : IInputUi
    {
        public bool DrawInputEdit(string name, InputValue inputValue)
        {
            if (inputValue is InputValue<float> floatValue)
            {
                return ImGui.DragFloat(name, ref floatValue.Value);
            }
            else
            {
                Debug.Assert(false);
                return false;
            }
        }
    }

    public class IntInputUi : IInputUi
    {
        public bool DrawInputEdit(string name, InputValue inputValue)
        {
            if (inputValue is InputValue<int> intValue)
            {
                return ImGui.DragInt(name, ref intValue.Value);
            }
            else
            {
                Debug.Assert(false);
                return false;
            }
        }
    }

    public static class InputUiRegistry
    {
        public static Dictionary<Type, IInputUi> Entries { get; } = new Dictionary<Type, IInputUi>();
    }

}