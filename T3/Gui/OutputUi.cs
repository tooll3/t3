using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Gui.Selection;

namespace T3.Gui
{
    public interface IOutputUi : ISelectable
    {
        void DrawValue(Slot slot);
        Type Type { get; }
    }

    public class ValueOutputUi<T> : IOutputUi
    {
        public void DrawValue(Slot slot)
        {
            if (slot is Slot<T> typedSlot)
            {
                var value = typedSlot.GetValue(new EvaluationContext());
                ImGui.Text($"{value}");
            }
            else
            {
                Debug.Assert(false);
            }
        }

        public Type Type { get; } = typeof(T);
        public Vector2 Position { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = new Vector2(100, 30);
        public bool IsSelected { get; set; }
    }

    public class FloatOutputUi : ValueOutputUi<float>
    {
    }

    public class IntOutputUi : ValueOutputUi<int>
    {
    }

    public class StringOutputUi : ValueOutputUi<string>
    {
    }

    public static class OutputUiRegistry
    {
        public static Dictionary<Guid, Dictionary<Guid, IOutputUi>> Entries { get; } = new Dictionary<Guid, Dictionary<Guid, IOutputUi>>();
    }

    public static class OutputUiFactory
    {
        public static Dictionary<Type, Func<IOutputUi>> Entries { get; } = new Dictionary<Type, Func<IOutputUi>>();
    }
}