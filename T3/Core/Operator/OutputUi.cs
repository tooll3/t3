using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using T3.Gui;

namespace T3.Core.Operator
{
    public interface IOutputUi
    {
        void DrawValue(Slot slot);

        Vector2 Position { get; set; }
        Vector2 Size { get; set; }
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

        public Vector2 Position { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = new Vector2(100, 30);
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

        public static Dictionary<Type, IOutputUi> EntriesByType { get; } = new Dictionary<Type, IOutputUi>();
    }

}