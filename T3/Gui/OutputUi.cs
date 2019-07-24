using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Selection;
using Vector2 = System.Numerics.Vector2;

namespace T3.Gui
{
    public interface IOutputUi : ISelectable
    {
        void DrawValue(Slot slot);
        Type Type { get; }
    }

    public class ValueOutputUi<T> : IOutputUi
    {
        public Guid Id { get; }

        public ValueOutputUi(Guid id)
        {
            Id = id;
        }

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
        public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = new Vector2(100, 30);
        public bool IsSelected { get; set; }
    }

    // todo: refactor out common code with ValueOutputUi<T> - it's nearly the same
    public class ShaderResourceViewOutputUi : IOutputUi
    {
        public Guid Id { get; }

        public ShaderResourceViewOutputUi(Guid id)
        {
            Id = id;
        }

        public void DrawValue(Slot slot)
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

        public Type Type { get; } = typeof(ShaderResourceView);
        public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = new Vector2(100, 30);
        public bool IsSelected { get; set; }
    }

    public class FloatOutputUi : ValueOutputUi<float>
    {
        public FloatOutputUi(Guid id) : base(id)
        {
        }
    }

    public class IntOutputUi : ValueOutputUi<int>
    {
        public IntOutputUi(Guid id) : base(id)
        {
        }
    }

    public class StringOutputUi : ValueOutputUi<string>
    {
        public StringOutputUi(Guid id) : base(id)
        {
        }
    }

    public class Size2OutputUi : ValueOutputUi<Size2>
    {
        public Size2OutputUi(Guid id) : base(id)
        {
        }
    }

    public class Texture2dOutputUi : ValueOutputUi<Texture2D>
    {
        public Texture2dOutputUi(Guid id) : base(id)
        {
        }
    }

    public static class OutputUiFactory
    {
        public static Dictionary<Type, Func<Guid, IOutputUi>> Entries { get; } = new Dictionary<Type, Func<Guid, IOutputUi>>();
    }
}