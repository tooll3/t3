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
    public sealed class EnumTypeExtensions : ITypeExtension
    {
        private static object JsonToEnumValue(JToken jsonToken, Type enumType)
        {
            string value = jsonToken.Value<string>();

            if (Enum.TryParse(enumType, value, out object? enumValue))
            {
                return enumValue;
            }
            else
            {
                return null;
            }
        }

        public void RegisterPersistedTypes(Action<PersistedTypeDescription> registerAction)
        {
            var enumTypes = Assembly.GetExecutingAssembly().GetExportedTypes().Where(t => t.IsEnum && t.GetCustomAttribute<T3TypeAttribute>() != null);

            foreach (var enumType in enumTypes)
            {
                
                T3TypeAttribute attribute = enumType.GetCustomAttribute<T3TypeAttribute>();
                string typeName = attribute.TypeName;
                if (string.IsNullOrEmpty(attribute.TypeName))
                {
                    typeName = enumType.Name;
                }

                Type inputValueType = typeof(InputValue<>).MakeGenericType(enumType);
                registerAction(new PersistedTypeDescription()
                {
                    Type = enumType,
                    TypeName = typeName,
                    DefaultValueCreator = () => Activator.CreateInstance(inputValueType) as InputValue,
                    ValueToJsonConverter = (writer, obj) => writer.WriteValue(obj.ToString()),
                    JsonToValueConverter = tok => JsonToEnumValue(tok, enumType)
                });
            }
        }

        public void RegisterTypes(Action<TypeDescription> registerAction)
        {
            
        }
    }
}
