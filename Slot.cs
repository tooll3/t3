using System;
using System.Data;
using System.Linq;

namespace Tooll.Core.PullVariant
{

    public class EvaluationContext
    {
    }

    class Slot<T>
    {
        protected static Action<EvaluationContext> EmptyUpdateAction = delegate { };
        //public event EventHandler<bool> ChangedEvent;
/*
        public Slot()
        {
            _updateAction = _emptyUpdateAction;
        }
*/

        public Slot(Action<EvaluationContext> updateAction)
        {
            UpdateAction = updateAction;
        }

        public Slot(T defaultValue)
        {
            Value = defaultValue;
        }

        public T GetValue(EvaluationContext context)
        {
            UpdateAction(context);
            return Value;
        }

        //private bool _changed;
        //public bool Changed
        //{
        //    get { return _changed; }
        //    set
        //    {
        //        _changed = value;
        //        if (Changed && ChangedEvent != null)
        //        {
        //            ChangedEvent(this, true);
        //        }
        //    }
        //}

        protected Action<EvaluationContext> UpdateAction;
        private T _value;

        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                //Changed = true;
            }
        }
    }


    class InputSlot<T> : Slot<T>
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

        public Slot<T> Input { get; set; }
    }

    class TestOperator
    {
        //
        public Slot<float> Result { get; }

        public TestOperator()
        {
            Result = new Slot<float>(Update) { Value = 17.0f };
            Count = new InputSlot<int>(1);
            Bla = new InputSlot<string>("hahaha");
            IntArray = new InputSlot<int[]>(new [] {1,2,3,4,5,6});
        }

        public void Update(EvaluationContext context)
        {
            // Result.Value = Count.GetValue(context) * (Bla.GetValue(context) == "Hallo" ? 1 : 2)*IntArray.GetValue(context).Sum();
            // or
            Count.Update(context);
            Bla.Update(context);
            IntArray.Update(context);
            Result.Value = Count.Value*(Bla.Value == "Hallo" ? 1 : 2)*IntArray.Value.Sum();
        }

        //[Operator.Input]
        public InputSlot<int> Count { get; }

        //[Operator.Input]
        public InputSlot<string> Bla { get; }

        //[Operator.Input]
        public InputSlot<int[]> IntArray { get; }
    }

}