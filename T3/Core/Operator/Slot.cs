using System;
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

        public OperatorAttribute(Type type)
        {
            Type = type;
        }

        protected OperatorAttribute()
        {
        }

        public Type Type { get; set; }

    }

    public class Slot
    {
        public string Name { get; set; } = String.Empty;
        public Type Type { get; protected set; }
    }

    public abstract class InputValue
    {
        public Type ValueType;
    }

    public class InputValue<T> : InputValue
    {
        public InputValue(T value)
        {
            Value = value;
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
            if (IsDirty)
            {
                UpdateAction(context);
//                 IsDirty = false;
            }
            return Value;
        }

        public Action<EvaluationContext> UpdateAction;
    }


    interface IInputSlot
    {
        InputValue InputValue { get; set; }
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
            Value = Input != null ? Input.GetValue(context) : TypedInputValue.Value;
        }

        private Slot<T> _input;
        public Slot<T> Input
        {
            get => _input;
            set
            {
                _input = value;
                IsDirty = true;
            }
        }

        public InputValue<T> TypedInputValue;
        public InputValue InputValue
        {
            get => TypedInputValue;
            set => TypedInputValue = (InputValue<T>)value;
        }
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
            if (Input != null)
                Value = Input.GetValue(context);
            else
            {
                if (Width.Input != null)
                    Value.Width = Width.GetValue(context);
                if (Height.Input != null)
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