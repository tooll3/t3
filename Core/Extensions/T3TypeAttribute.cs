using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T3.Core
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class T3TypeAttribute : Attribute
    {
        public readonly string TypeName;

        public T3TypeAttribute(string typeName)
        {
            TypeName = typeName;
        }

        public T3TypeAttribute()
        {
            TypeName = null;
        }
    }
}
