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
            var tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
        }

        public static void Swap<T>(ref T a, ref T b)
        {
            T tmp = a;
            a = b;
            b = tmp;
        }

        public static T GetEnumValue<T>(this InputSlot<int> intInputSlot, EvaluationContext context) where T : Enum
        {
            return CastTo<T>.From(intInputSlot.GetValue(context));
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