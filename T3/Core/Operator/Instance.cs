using System;
using System.Collections.Generic;
using System.Linq;

namespace T3.Core.Operator
{
    public abstract class Instance : IDisposable
    {
        public abstract Type Type { get; }
        public Guid Id;
        public Instance Parent { get; internal set; }
        public Symbol Symbol { get; internal set; }

        public List<Slot> Outputs { get; set; } = new List<Slot>();
        public List<Instance> Children { get; set; } = new List<Instance>();
        public List<IInputSlot> Inputs { get; set; } = new List<IInputSlot>();

        public void Dispose()
        {
        }

        protected void SetupInputAndOutputsFromType()
        {
            // input identified by base interface
            Type inputSlotType = typeof(IInputSlot);
            var inputInfos = from field in Type.GetFields()
                             where inputSlotType.IsAssignableFrom(field.FieldType)
                             select field;
            foreach (var inputInfo in inputInfos)
            {
                Inputs.Add((IInputSlot)inputInfo.GetValue(this));
            }

            // outputs identified by attribute
            var outputs = (from field in Type.GetFields()
                          let attributes = field.GetCustomAttributes(typeof(OperatorAttribute), false)
                          from attr in attributes
                          select field).ToArray();
            foreach (var output in outputs)
            {
                Outputs.Add((Slot)output.GetValue(this));
            }
        }
    }

    public class Instance<T> : Instance
    {
        public override Type Type => typeof(T);

        public Instance()
        {
            SetupInputAndOutputsFromType();
        }
    }
}
