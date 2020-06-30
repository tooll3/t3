using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace T3.Gui.UiHelpers
{
    public class EnumCache
    {
        public static EnumCache Instance { get; } = new EnumCache();

        public Entry<T> GetEnumEntry<T>() where T : Enum
        {
            Type enumType = typeof(T);
            if (!_entries.TryGetValue(enumType, out var entry))
            {
                entry = new Entry<T>();
                _entries.Add(enumType, entry);
            }

            return (Entry<T>)entry;
        }

        public class Entry<T> where T : Enum
        {
            public Entry()
            {
                var enumType = typeof(T);
                ValueNames = Enum.GetNames(enumType);
                var values = Enum.GetValues(enumType);
                Values = new T[ValueNames.Length];
                ValuesAsInt = new int[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    Values[i] = (T)values.GetValue(i);
                    ValuesAsInt[i] = (int)values.GetValue(i);
                }

                IsFlagEnum = enumType.GetCustomAttributes<FlagsAttribute>().Any();
                if (IsFlagEnum)
                {
                    SetFlags = new bool[values.Length];
                }
            }

            public T this[int i] => Values[i];
            public T[] Values { get; }
            public string[] ValueNames { get; }
            public int[] ValuesAsInt { get; }
            public bool IsFlagEnum { get; }
            public bool[] SetFlags { get; }
        }

        private readonly Dictionary<Type, object> _entries = new Dictionary<Type, object>();
    }
}