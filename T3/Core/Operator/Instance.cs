using System;
using System.Collections.Generic;

namespace T3.Core.Operator
{
    public class Instance : IDisposable
    {
        public Instance Parent { get; internal set; }
        public Symbol Symbol { get; internal set; }
        public Guid Id;

        public void Dispose()
        {
        }

        public List<Slot> Outputs { get; set; } = new List<Slot>();
        public List<Instance> Children { get; set; } = new List<Instance>();
        public List<Slot> Inputs { get; set; } = new List<Slot>();
    }
}
