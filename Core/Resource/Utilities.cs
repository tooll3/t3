using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using T3.Core.Operator;
using T3.Core.Operator.Slots;

namespace T3.Core
{
    public static class Utilities
    {
        public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
        {
            key = tuple.Key;
            value = tuple.Value;
        }

        public static void Dispose<T>(ref T obj) where T : class, IDisposable
        {
            obj?.Dispose();
            obj = null;
        }

        public static void Swap<T>(this IList<T> list, int indexA, int indexB)
        {
            (list[indexA], list[indexB]) = (list[indexB], list[indexA]);
        }

        public static void Swap<T>(ref T a, ref T b)
        {
            (a, b) = (b, a);
        }

        public static float[] GetFloatsFromVector<T>(T v)
        {
            if (v is float v1)
            {
                return new[] { v1 };
            }
            
            if (v is System.Numerics.Vector2 v2)
            {
                return new[] { v2.X, v2.Y };
            }
            
            if (v is System.Numerics.Vector3 v3)
            {
                return new[] { v3.X, v3.Y, v3.Z };
            }

            if (v is System.Numerics.Vector4 v4)
            {
                return new[] { v4.X, v4.Y, v4.Z, v4.W };
            }
            return Array.Empty<float>();
        }
        
        
        public static T GetEnumValue<T>(this InputSlot<int> intInputSlot, EvaluationContext context) where T : Enum
        {
            return CastTo<T>.From(intInputSlot.GetValue(context));
        }
        
        public static int Hash<T>(T a, T b)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + a.GetHashCode();
                hash = hash * 31 + b.GetHashCode();
                return hash;
            }
        }
    }

    public static class CastTo<TTarget>
    {
        public static TTarget From<TSource>(TSource source)
        {
            return Cache<TSource>.Caster(source);
        }

        private static class Cache<TSource>
        {
            public static readonly Func<TSource, TTarget> Caster = Get();

            private static Func<TSource, TTarget> Get()
            {
                var p = Expression.Parameter(typeof(TSource));
                var c = Expression.ConvertChecked(p, typeof(TTarget));
                return Expression.Lambda<Func<TSource, TTarget>>(c, p).Compile();
            }
        }
    }
}