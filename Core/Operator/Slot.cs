using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using T3.Core.Logging;

namespace T3.Core.Operator
{
    public class EvaluationContext
    {
        public EvaluationContext()
        {
            Time = GlobalTime;
        }

        public void Reset()
        {
            Time = GlobalTime;
        }

        public static double GlobalTime { get; set; }
        public double Time { get; set; }
    }

    public class OperatorAttribute : Attribute
    {
        public Guid Id { get; set; }
        public string Guid { get => Id.ToString(); set => Id = System.Guid.Parse(value); }
    }

    public class OutputAttribute : OperatorAttribute
    {
    }

    public class InputAttribute : OperatorAttribute
    {
        public Type Type { get; set; }
    }

    [Flags]
    public enum DirtyFlagTrigger
    {
        None = 0,
        Time = 0x1,
        Animated = 0x2,
    }

    public class DirtyFlag
    {
        public bool IsDirty => Reference != Target;

        public void Invalidate()
        {
            Target++;
        }

        public void Clear()
        {
            Reference = Target;
        }

        public int Reference = 0;
        public int Target = 1;
        public DirtyFlagTrigger Trigger;
    }

    public interface ISlot
    {
        Guid Id { get; set; }
        Type Type { get; }
        Instance Parent { get; set; }
        DirtyFlag DirtyFlag { get; set; }
        void AddConnection(ISlot source, int index = 0);
        void RemoveConnection(int index = 0);
        bool IsConnected { get; }
        ISlot GetConnection(int index);
    }

    public abstract class InputValue
    {
        public Type ValueType;
        public abstract InputValue Clone();
        public abstract void Assign(InputValue otherValue);
        public abstract void ToJson(JsonTextWriter writer);
        public abstract void SetValueFromJson(JToken json);
    }

    public class InputValue<T> : InputValue
    {
        public InputValue() : this(default)
        {
        }

        public InputValue(T value)
        {
            Value = value;
            ValueType = typeof(T);
        }

        public override InputValue Clone()
        {
            return new InputValue<T>(Value);
        }

        public override void Assign(InputValue otherValue)
        {
            if (otherValue is InputValue<T> otherTypedValue)
            {
                Value = otherTypedValue.Value;
            }
            else
            {
                Debug.Assert(false); // trying to assign different types of input values
            }
        }

        public override void ToJson(JsonTextWriter writer)
        {
            if (TypeValueToJsonConverters.Entries.TryGetValue(ValueType, out var converterFunc))
            {
                converterFunc(writer, Value);
            }
            else
            {
                Log.Error($"Trying to convert an input value of type '{ValueType}' but no converter registered in TypeValueToJsonConverters. Returning empty string.");
            }
        }

        public override void SetValueFromJson(Newtonsoft.Json.Linq.JToken json)
        {
            if (JsonToTypeValueConverters.Entries.TryGetValue(ValueType, out var converterFunc))
            {
                Value = (T)converterFunc(json);
            }
            else
            {
                Log.Error($"Trying to read a json value for type '{ValueType}' but no converter registered in JsonToTypeValueConverters. Skipping value reading.");
            }
        }

        public T Value;
    }

    public class Slot<T> : ISlot
    {
        public Guid Id { get; set; }
        public Type Type { get; protected set; }
        public Instance Parent { get; set; }
        public DirtyFlag DirtyFlag { get; set; } = new DirtyFlag();
        
        public T Value; // { get; set; }
        public bool IsMultiInput { get; protected set; } = false;

        public Slot()
        {
            UpdateAction = Update;
            Type = typeof(T);
        }

        public Slot(Action<EvaluationContext> updateAction) : this()
        {
            UpdateAction = updateAction;
        }

        public Slot(Action<EvaluationContext> updateAction, T defaultValue) : this()
        {
            UpdateAction = updateAction;
            Value = defaultValue;
        }

        public Slot(T defaultValue) : this()
        {
            Value = defaultValue;
        }

        public void Update(EvaluationContext context)
        {
        }

        public void ConnectedUpdate(EvaluationContext context)
        {
            Value = InputConnection[0].GetValue(context);
        }

        public T GetValue(EvaluationContext context)
        {
            if (DirtyFlag.IsDirty)
            {
                UpdateAction?.Invoke(context);
                DirtyFlag.Clear();
            }

            return Value;
        }

        public void AddConnection(ISlot sourceSlot, int index = 0)
        {
            if (!IsConnected)
            {
                UpdateAction = ConnectedUpdate;
                DirtyFlag.Target = sourceSlot.DirtyFlag.Target;
                DirtyFlag.Reference = DirtyFlag.Target - 1;
            }

            InputConnection.Insert(index, (Slot<T>)sourceSlot);
        }

        public void RemoveConnection(int index = 0)
        {
            if (IsConnected)
            {
                if (index < InputConnection.Count)
                {
                    InputConnection.RemoveAt(index);
                }
                else
                {
                    Log.Error($"Trying to delete connection at index {index}, but input slot only has {InputConnection.Count} connections");
                }
            }

            if (!IsConnected)
            {
                // if no connection is set anymore restore the default update action
                SetUpdateActionBackToDefault();
                DirtyFlag.Invalidate();
            }
        }

        public void SetUpdateActionBackToDefault()
        {
            UpdateAction = _defaultUpdateAction;
        }

        public bool IsConnected => InputConnection.Count > 0;

        public ISlot GetConnection(int index)
        {
            return InputConnection[index];
        }

        private List<Slot<T>> _inputConnection = new List<Slot<T>>();

        public List<Slot<T>> InputConnection
        {
            get => _inputConnection;
            set
            {
                _inputConnection = value;
                DirtyFlag.Target++;
            }
        }

        public Action<EvaluationContext> UpdateAction;
        protected Action<EvaluationContext> _defaultUpdateAction;
    }

    public interface IOutputSlot
    {
        Guid Id { get; }
    }

    public interface IInputSlot : ISlot
    {
        SymbolChild.Input Input { get; set; }
        bool IsMultiInput { get; }
    }

    public class InputSlot<T> : Slot<T>, IInputSlot
    {
        public InputSlot(InputValue<T> typedInputValue)
        {
            UpdateAction = InputUpdate;
            _defaultUpdateAction = UpdateAction;
            TypedInputValue = typedInputValue;
            Value = typedInputValue.Value;
        }

        public InputSlot() : this(default(T))
        {
            UpdateAction = InputUpdate;
            _defaultUpdateAction = UpdateAction;
        }

        public InputSlot(T value) : this(new InputValue<T>(value))
        {
        }

        public void InputUpdate(EvaluationContext context)
        {
            Value = Input.IsDefault ? TypedDefaultValue.Value : TypedInputValue.Value;
        }

        private SymbolChild.Input _input;

        public SymbolChild.Input Input
        {
            get => _input;
            set
            {
                _input = value;
                TypedInputValue = (InputValue<T>)value.Value;
                TypedDefaultValue = (InputValue<T>)value.DefaultValue;
            }
        }

        public InputValue<T> TypedInputValue;
        public InputValue<T> TypedDefaultValue;
    }

    public interface IMultiInputSlot : IInputSlot
    {
        List<ISlot> GetCollectedInputs2();
    }

    public class MultiInputSlot<T> : InputSlot<T>, IMultiInputSlot
    {
        public List<Slot<T>> CollectedInputs { get; } = new List<Slot<T>>(10);
        public List<ISlot> CollectedInputs2 { get; } = new List<ISlot>(10);

        public MultiInputSlot(InputValue<T> typedInputValue) : base(typedInputValue)
        {
            IsMultiInput = true;
        }

        public MultiInputSlot()
        {
            IsMultiInput = true;
        }

        public List<Slot<T>> GetCollectedInputs()
        {
            CollectedInputs.Clear();

            foreach (var slot in InputConnection)
            {
                if (slot.IsMultiInput && slot.IsConnected)
                {
                    var multiInput = (MultiInputSlot<T>)slot;
                    CollectedInputs.AddRange(multiInput.GetCollectedInputs());
                }
                else
                {
                    CollectedInputs.Add(slot);
                }
            }

            return CollectedInputs;
        }

        public List<ISlot> GetCollectedInputs2()
        {
            CollectedInputs2.Clear();

            foreach (var slot in InputConnection)
            {
                if (slot.IsMultiInput && slot.IsConnected)
                {
                    var multiInput = (IMultiInputSlot)slot;
                    CollectedInputs2.AddRange(multiInput.GetCollectedInputs2());
                }
                else
                {
                    CollectedInputs2.Add(slot);
                }
            }

            return CollectedInputs2;
        }
    }

    public class Size2Slot : InputSlot<Size2>
    {
        public Size2Slot(Size2 defaultValue) : base(defaultValue)
        {
            UpdateAction = Update;
            _defaultUpdateAction = UpdateAction;
        }

        public Size2Slot(InputValue<Size2> typedInputValue) : base(typedInputValue)
        {
            UpdateAction = Update;
            _defaultUpdateAction = UpdateAction;
        }

        public new void Update(EvaluationContext context)
        {
            if (InputConnection.Count > 0)
                Value = InputConnection[0].GetValue(context);
            else
            {
                if (Width.InputConnection != null)
                    Value.Width = Width.GetValue(context);
                if (Height.InputConnection != null)
                    Value.Height = Height.GetValue(context);
            }
        }

        public InputSlot<int> Width = new InputSlot<int>(new InputValue<int>(0));
        public InputSlot<int> Height = new InputSlot<int>(new InputValue<int>(0));
    }

    public class ConverterSlot<TFrom, TTo> : Slot<TTo>
    {
        readonly Func<TFrom, TTo> _converterFunc;

        public ConverterSlot(Slot<TFrom> sourceSlot, Func<TFrom, TTo> converterFunc)
        {
            UpdateAction = Update;
            _defaultUpdateAction = UpdateAction;
            SourceSlot = sourceSlot;
            //var floatToInt = new Converter2<float, int>(f => (int)f);
            _converterFunc = converterFunc;
        }

        private Slot<TFrom> SourceSlot { get; }

        public new void Update(EvaluationContext context)
        {
            Value = _converterFunc(SourceSlot.GetValue(context));
        }
    }
}