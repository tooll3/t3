using System;

namespace T3.Core.Operator.Slots.Research
{
    public sealed class ConverterSlot<TFrom, TTo> : Slot<TTo>
    {
        readonly Func<TFrom, TTo> _converterFunc;

        public ConverterSlot(Slot<TFrom> sourceSlot, Func<TFrom, TTo> converterFunc)
        {
            UpdateAction = Update;
            _keepOriginalUpdateAction = UpdateAction;
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