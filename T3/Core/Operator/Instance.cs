using System;
using System.Collections.Generic;

namespace T3.Core.Operator
{
    public class Instance : IDisposable
    {
        public Instance Parent { get; internal set; }
        public Symbol Definition { get; internal set; }

        public void Dispose()
        {
        }

        public List<Slot> Outputs { get; } = new List<Slot>();
        public List<Instance> Children { get; } = new List<Instance>();
        public List<Slot> Inputs { get; } = new List<Slot>();
    }
}
