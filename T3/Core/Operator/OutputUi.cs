using System;
using System.Collections.Generic;
using System.Diagnostics;
using ImGuiNET;
using T3.Gui;

namespace T3.Core.Operator
{

    public interface IOutputUi
    {
        void Draw(Slot slot);
    }

    public class ValueOutputUi<T> : IOutputUi
    {
        public void Draw(Slot slot)
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
        public static Dictionary<Type, IOutputUi> Entries { get; } = new Dictionary<Type, IOutputUi>();
    }

}