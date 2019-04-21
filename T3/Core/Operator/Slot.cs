using System;
using System.Diagnostics;
using System.Reflection;
using SharpDX;

namespace T3.Core.Operator
{

    public class EvaluationContext
    {
    }

//    public abstract class OperatorBaseAttribute : Attribute
//    {
//    }

    public class OperatorAttribute : Attribute
    {
        public enum OperatorType
        {
            Input,
            Output
        }

        public OperatorAttribute(OperatorType type)
        {
            Type = type;
        }

        protected OperatorAttribute()
        {
        }

        public OperatorType Type { get; set; }

    }

    public class Slot
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = String.Empty;
        public Type Type { get; protected set; }
    }

    public abstract class InputValue
    {
        public Type ValueType;
        public abstract InputValue Clone();
        public abstract void Assign(InputValue otherValue);
    }

    public class InputValue<T> : InputValue
    {
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

        public T Value;
    }

    public class Slot<T> : Slot
    {
        protected static Action<EvaluationContext> EmptyUpdateAction = delegate { };

        public T Value;// { get; set; }
        public bool IsDirty { get; set; } = true;

        public Slot() 
        { 
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
            UpdateAction = EmptyUpdateAction;
            Value = defaultValue;
        }

        public T GetValue(EvaluationContext context)
        {
//             if (IsDirty)
            {
                UpdateAction(context);
//                 IsDirty = false;
            }
            return Value;
        }

        public Action<EvaluationContext> UpdateAction;
    }

    public interface IOutputSlot
    {
        Guid Id { get; }

    }


    public interface IInputSlot
    {
//         InputValue InputValue { get; set; }
//         InputValue DefaultValue { get; set; }
        Guid Id { get; set; }
        SymbolChild.Input Input { set; }
        void AddConnection(Slot slot);
        void RemoveConnection();
    }

    public class InputSlot<T> : Slot<T>, IInputSlot
    {
        public InputSlot(InputValue<T> typedInputValue)
        {
            UpdateAction = Update;
            TypedInputValue = typedInputValue;
        }

        public InputSlot(T defaultValue)
            : this(new InputValue<T>(defaultValue))
        {
        }

        public void Update(EvaluationContext context)
        {
            Value = InputConnection != null ? InputConnection.GetValue(context)
                                            : Input.IsDefault ? TypedDefaultValue.Value
                                                              : TypedInputValue.Value;
        }

        public void AddConnection(Slot slot)
        {
            InputConnection = (Slot<T>)slot;
        }

        public void RemoveConnection()
        {
            InputConnection = null;
        }

        private Slot<T> _inputConnection;
        public Slot<T> InputConnection
        {
            get => _inputConnection;
            set
            {
                _inputConnection = value;
                IsDirty = true;
            }
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

    public class Size2Slot : InputSlot<Size2>
    {
        public Size2Slot(Size2 defaultValue)
        : base(defaultValue)
        {
            UpdateAction = Update;
        }

        public Size2Slot(InputValue<Size2> typedInputValue)
            : base(typedInputValue)
        {
            UpdateAction = Update;
        }

        public new void Update(EvaluationContext context)
        {
            if (InputConnection != null)
                Value = InputConnection.GetValue(context);
            else
            {
                if (Width.InputConnection != null)
                    Value.Width = Width.GetValue(context);
                if (Height.InputConnection != null)
                    Value.Height = Height.GetValue(context);
            }
        }

        public InputSlot<int> Width  = new InputSlot<int>(new InputValue<int>(0));
        public InputSlot<int> Height = new InputSlot<int>(new InputValue<int>(0));
    }


    public class ConverterSlot<TFrom, TTo> : Slot<TTo> 
    {
        readonly Func<TFrom, TTo> _converterFunc;

        public ConverterSlot(Slot<TFrom> sourceSlot, Func<TFrom, TTo> converterFunc)                                                                            
        {
            UpdateAction = Update;
            SourceSlot = sourceSlot;
            //var floatToInt = new Converter2<float, int>(f => (int)f);
            _converterFunc = converterFunc;
        }

        private Slot<TFrom> SourceSlot { get; }

        public void Update(EvaluationContext context)
        {
            Value = _converterFunc(SourceSlot.GetValue(context));
        }
    }


}