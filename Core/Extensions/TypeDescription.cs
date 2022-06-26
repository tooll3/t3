using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using T3.Core.Operator;

namespace T3.Core.Extensions
{
    public struct TypeDescription
    {
        public Type Type;
        public string TypeName;
        public Func<InputValue> DefaultValueCreator;
    }
}
