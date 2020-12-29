using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public abstract object this[int i] { get; set; }
        public abstract int NumElements { get; }
        public abstract int ElementSizeInBytes { get; }
        public abstract int TotalSizeInBytes { get; }
        public abstract void WriteToStream(DataStream stream);
        public abstract StructuredList Clone();
        public abstract StructuredList Join(params StructuredList[] other);
        // public abstract StructuredList Filter(Func<object, bool> filter);
    }

    public class StructuredList<T> : StructuredList where T : struct
    {
        public StructuredList(int count) : base(typeof(T))
        {
            TypedElements = new T[count];
        }

        public T[] TypedElements { get; }
        
        public override object Elements => TypedElements;

        public override object this[int i]
        {
            get
            {
                if (i >= 0 && i < NumElements)
                {
                    return TypedElements[i];
                }

                Log.Debug($"StructuredList.GetElement: Tried to use index {i}, but length is {TypedElements.Length}.");
                return null;
            }
            set => TypedElements[i] = (T)value;
        }

        public override int NumElements => TypedElements.Length;
        public override int ElementSizeInBytes => Marshal.SizeOf<T>();
        public override int TotalSizeInBytes => NumElements * ElementSizeInBytes;

        public override void WriteToStream(DataStream stream)
        {
            stream.WriteRange(TypedElements);
        }

        public override StructuredList Clone()
        {
            var clone = new StructuredList<T>(NumElements);
            Array.Copy(TypedElements, clone.TypedElements, NumElements);

            return clone;
        }

        public override StructuredList Join(params StructuredList[] lists)
        {
            int count = TypedElements.Length;
            for (int i = 0; i < lists.Length; i++)
            {
                if (Type != lists[i].Type)
                {
                    Log.Warning($"StructuredList.Join: trying to join different structure types, skipping at index {i}.");
                    continue;
                }

                count += lists[i].NumElements;
            }

            var newList = new StructuredList<T>(count);
            Array.Copy(TypedElements, newList.TypedElements, NumElements);
            int startIndex = TypedElements.Length;
            foreach (var sourceList in lists)
            {
                var sourceArray = (T[])sourceList.Elements;
                var numElements = sourceList.NumElements;
                Array.Copy(sourceArray, 0, newList.TypedElements, startIndex, numElements);
                startIndex += numElements;
            }

            return newList;
        }

        // public override StructuredList Filter(Func<object, bool> filter)
        // {
        // }
    }
}