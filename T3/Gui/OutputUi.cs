using ImGuiNET;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using T3.Core.Operator;
using T3.Gui.Selection;
using Vector2 = System.Numerics.Vector2;

namespace T3.Gui
{
    public interface IOutputUi : ISelectable
    {
        Symbol.OutputDefinition OutputDefinition { get; set; }
        Type Type { get; }
        void DrawValue(Slot slot);
    }

    public abstract class OutputUi<T> : IOutputUi
    {
        public Symbol.OutputDefinition OutputDefinition { get; set; }
        public Guid Id => OutputDefinition.Id;
        public Type Type { get; } = typeof(T);
        public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = new Vector2(100, 30);
        public bool IsSelected { get; set; }
        
        public abstract void DrawValue(Slot slot);

    }

    public class ValueOutputUi<T> : OutputUi<T>
    {
        public override void DrawValue(Slot slot)
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

    public class ShaderResourceViewOutputUi : OutputUi<ShaderResourceView>
    {
        public override void DrawValue(Slot slot)
        {
            if (slot is Slot<ShaderResourceView> typedSlot)
            {
                var value = typedSlot.GetValue(new EvaluationContext());
                ImGui.Image((IntPtr)value, new Vector2(100.0f, 100.0f));
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

    public class FloatListOutputUi : OutputUi<List<float>>
    {
        public override void DrawValue(Slot slot)
        {
            if (slot is Slot<List<float>> typedSlot)
            {
                var list = typedSlot.GetValue(new EvaluationContext());
                var outputString = string.Join(", ", list);
                ImGui.Text($"{outputString}");
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }

    public class IntOutputUi : ValueOutputUi<int>
    {
    }

    public class StringOutputUi : ValueOutputUi<string>
    {
    }

    public class Size2OutputUi : ValueOutputUi<Size2>
    {
    }

    public class Texture2dOutputUi : ValueOutputUi<Texture2D>
    {
    }

    public static class OutputUiFactory
    {
        public static Dictionary<Type, Func<IOutputUi>> Entries { get; } = new Dictionary<Type, Func<IOutputUi>>();
    }
}