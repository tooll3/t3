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
            return Assembly.GetExecutingAssembly().GetExportedTypes().Where(t => typePredicate(t));
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
    }
}
