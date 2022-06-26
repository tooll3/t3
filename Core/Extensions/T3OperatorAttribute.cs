using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T3.Core.Extensions
{
    public sealed class T3OperatorAttribute : Attribute
    {
        private readonly string name;
        private readonly Guid id;

        public string Name => this.name;

        public Guid Id => this.id;

        public T3OperatorAttribute(string id, string name)
        {
            this.name = name;
            this.id = new Guid(id);
        }
    }
}
