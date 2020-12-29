using System;
using System.Runtime.InteropServices;
using SharpDX;
using T3.Core.Logging;

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
        public abstract object GetElement(int index);
        public abstract int ElementSizeInBytes { get; }
        public abstract int TotalSizeInBytes { get; }
        public abstract void WriteToStream(DataStream stream);

        public int GetCount()
        {
            return TotalSizeInBytes / ElementSizeInBytes;
        }
    }

    public class StructuredList<T> : StructuredList where T : struct
    {
        public StructuredList(int count) : base(typeof(T))
        {
            TypedElements = new T[count];
        }

        public T[] TypedElements { get; }
        
        public override object Elements => TypedElements;
        
        public override object GetElement(int index)
        {
            if (index >= 0 && index < TypedElements.Length)
            {
                return TypedElements[index];
            }

            Log.Debug($"StructuredList.GetElement: Tried to use index {index}, but length is {TypedElements.Length}.");
            return null;
        }

        public override int ElementSizeInBytes => Marshal.SizeOf<T>();
        public override int TotalSizeInBytes => TypedElements.Length * ElementSizeInBytes;

        public override void WriteToStream(DataStream stream)
        {
            stream.WriteRange(TypedElements);
        }
    }
}