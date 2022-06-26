using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using T3.Core.Operator;

namespace T3.Core.Extensions
{
    public sealed class DefaultTypeExtensions : ITypeExtension
    {

        public void RegisterPersistedTypes(Action<PersistedTypeDescription> registerAction)
        {

        }

        public void RegisterTypes(Action<TypeDescription> registerAction)
        {
            var classTypes = Assembly.GetExecutingAssembly().GetExportedTypes().Where(t => t.IsClass && !t.IsAbstract 
            && t.GetCustomAttribute<T3TypeAttribute>() != null 
            && t.GetCustomAttribute<T3PersistAttribute>() == null);

            foreach (var classType in classTypes)
            {

                T3TypeAttribute attribute = classType.GetCustomAttribute<T3TypeAttribute>();
                string typeName = attribute.TypeName;
                if (string.IsNullOrEmpty(attribute.TypeName))
                {
                    typeName = classType.Name;
                }

                Type inputValueType = typeof(InputValue<>).MakeGenericType(classType);
                registerAction(new TypeDescription()
                {
                    Type = classType,
                    TypeName = typeName,
                    DefaultValueCreator = () => Activator.CreateInstance(inputValueType) as InputValue
                });
            }
        }
    }
}
