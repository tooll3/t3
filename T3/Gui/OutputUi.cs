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
        void DrawValue(ISlot slot);
    }

    public abstract class OutputUi<T> : IOutputUi
    {
        public Symbol.OutputDefinition OutputDefinition { get; set; }
        public Guid Id => OutputDefinition.Id;
        public Type Type { get; } = typeof(T);
        public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = new Vector2(100, 30);
        public bool IsSelected { get; set; }
        
        public abstract void DrawValue(ISlot slot);

        public int Invalidate(ISlot slot)
        {
            Instance parent = slot.Parent;

            bool outputDirty = false;
            foreach (var input in parent.Inputs)
            {
                if (input.IsConnected)
                {
                    if (input.IsMultiInput)
                    {
                        var multiInput = (IMultiInputSlot)input;
                        int dirtySum = 0;
                        foreach (var entry in multiInput.GetCollectedInputs())
                        {
                            dirtySum += Invalidate(entry);
                        }

                        input.DirtyFlag.Target = dirtySum;
                    }
                    else
                    {
                        input.DirtyFlag.Target = Invalidate(input.GetConnection(0));
                    }
                }
                else if (input.DirtyFlag.Trigger != DirtyFlagTrigger.None)
                {
                    input.DirtyFlag.Invalidate();
                }

                outputDirty |= input.DirtyFlag.IsDirty;
            }

            if (outputDirty || slot.DirtyFlag.Trigger != DirtyFlagTrigger.None)
            {
                slot.DirtyFlag.Invalidate();
            }

            return slot.DirtyFlag.Target;
        }

        protected EvaluationContext _evaluationContext = new EvaluationContext();
    }

    public class ValueOutputUi<T> : OutputUi<T>
    {
        public override void DrawValue(ISlot slot)
        {
            if (slot is Slot<T> typedSlot)
            {
                Invalidate(slot);
                _evaluationContext.Reset();
                var value = typedSlot.GetValue(_evaluationContext);
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
        public override void DrawValue(ISlot slot)
        {
            if (slot is Slot<ShaderResourceView> typedSlot)
            {
                Invalidate(slot);
                _evaluationContext.Reset();
                var value = typedSlot.GetValue(_evaluationContext);
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
        public override void DrawValue(ISlot slot)
        {
            if (slot is Slot<List<float>> typedSlot)
            {
                Invalidate(slot);
                _evaluationContext.Reset();
                var list = typedSlot.GetValue(_evaluationContext);
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