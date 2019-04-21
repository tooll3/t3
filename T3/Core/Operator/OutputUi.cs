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
                typedSlot.UpdateAction(new EvaluationContext());
                ImGui.Text($"{typedSlot.Value}");
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