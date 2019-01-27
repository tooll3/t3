using System;
using SharpDX;


namespace Tooll.Core.PullVariant
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

    public class Slot<T>
    {
        protected static Action<EvaluationContext> EmptyUpdateAction = delegate { };

        public T Value;// { get; set; }
        public string Name { get; set; }
        public bool IsDirty { get; set; } = true;

        public Slot() { }

        public Slot(Action<EvaluationContext> updateAction)
        {
            UpdateAction = updateAction;
        }

        public Slot(Action<EvaluationContext> updateAction, T defaultValue)
        {
            UpdateAction = updateAction;
            Value = defaultValue;
        }

        public Slot(T defaultValue)
        {
            UpdateAction = EmptyUpdateAction;
            Value = defaultValue;
        }

        public T GetValue(EvaluationContext context)
        {
            if (IsDirty)
            {
                UpdateAction(context);
                IsDirty = false;
            }
            return Value;
        }

        public Action<EvaluationContext> UpdateAction;
    }


    interface IInputSlot
    {
    }


    public class InputSlot<T> : Slot<T>, IInputSlot
    {

        public InputSlot(T defaultValue)
            : base(defaultValue)
        {
            UpdateAction = Update;
        }

        public void Update(EvaluationContext context)
        {
            if (Input != null)
            {
                Value = Input.GetValue(context);
            }
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
    }


    public class Size2Slot : InputSlot<Size2>
    {
        public Size2Slot(Size2 defaultValue)
            : base(defaultValue)
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

        public InputSlot<int> Width  = new InputSlot<int>(0);
        public InputSlot<int> Height = new InputSlot<int>(0);
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