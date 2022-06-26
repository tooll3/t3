using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace T3.Core
{
    public static class ReflectionUtilities
    {
        public static IEnumerable<Type> GetAssemblyTypes(this Assembly assembly, Predicate<Type> typePredicate)
        {
            return assembly.GetExportedTypes().Where(t => typePredicate(t));
        }

        public static IReadOnlyList<Type> GetDomainTypes(this AppDomain appDomain, Predicate<Type> typePredicate)
        {
            List<Type> types = new List<Type>();
            foreach (var assembly in appDomain.GetAssemblies())
            {
                types.AddRange(assembly.GetAssemblyTypes(typePredicate));
            }
            return types;
        }

        public static IReadOnlyList<Type> GetCurrentDomainTypes(Predicate<Type> typePredicate)
        {
            return AppDomain.CurrentDomain.GetDomainTypes(typePredicate);
        }

        public static IEnumerable<Type> GetConcreteClassTypeOf<TInterface>(this Assembly assembly)
        {
            return assembly.GetAssemblyTypes(t => typeof(TInterface).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass);
        }


        public static IReadOnlyList<Type> GetConcreteClassTypeOf<TInterface>(this AppDomain appDomain)
        {
            List<Type> types = new List<Type>();
            foreach (var assembly in appDomain.GetAssemblies())
            {
                types.AddRange(assembly.GetConcreteClassTypeOf<TInterface>());
            }
            return types;
        }

        public static IReadOnlyList<Type> GetConcreteClassTypeOfFromDomain<TInterface>()
        {
            return AppDomain.CurrentDomain.GetConcreteClassTypeOf<TInterface>();
        }
    }
}
