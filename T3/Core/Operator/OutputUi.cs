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
    public class FloatOutputUi : IOutputUi
    {
        public void Draw(Slot slot)
        {
            if (slot is Slot<float> floatSlot)
            {
                floatSlot.UpdateAction(new EvaluationContext());
                ImGui.Text($"{floatSlot.Value}");
            }
            else
            {
                Debug.Assert(false); 
            }
        }
    }

    public class IntOutputUi : IOutputUi
    {
        public void Draw(Slot slot)
        {
            if (slot is Slot<int> intSlot)
            {
                intSlot.UpdateAction(new EvaluationContext());
                ImGui.Text($"{intSlot.Value}");
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }

    public static class OutputUiRegistry
    {
        public static Dictionary<Type, IOutputUi> Entries { get; } = new Dictionary<Type, IOutputUi>();
    }

}