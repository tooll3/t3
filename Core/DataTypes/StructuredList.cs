using System;

namespace T3.Core.DataTypes
{
    public abstract class StructuredList
    {
        public StructuredList(Type type)
        {
            Type = type;
        }

        public Type Type { get; }
        public abstract object Elements { get; }
    }
}