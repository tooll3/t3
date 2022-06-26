using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using t3.Gui.InputUi.SimpleInputUis;
using T3.Core;
using T3.Core.Operator;
using T3.Gui.InputUi;

namespace T3.Gui.Extensions
{
    public sealed class EnumUiTypeExtensions : IUiTypeExtension
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

        public void RegisterUiTypes(Action<UiTypeDescription> registerAction)
        {
            //use core assembly here
            var coreAssembly = typeof(Model).Assembly;
            var enumTypes = coreAssembly.GetExportedTypes().Where(t => t.IsEnum && t.GetCustomAttribute<T3TypeAttribute>() != null);

            foreach (var enumType in enumTypes)
            {

                T3TypeAttribute attribute = enumType.GetCustomAttribute<T3TypeAttribute>();
                string typeName = attribute.TypeName;
                if (string.IsNullOrEmpty(attribute.TypeName))
                {
                    typeName = enumType.Name;
                }

                registerAction(new UiTypeDescription()
                {
                    Type = enumType,
                    UiProperties = new FallBackUiProperties(),
                    InputUi = () => Activator.CreateInstance(typeof(EnumInputUi<>).MakeGenericType(enumType)) as IInputUi,
                    OutputUi = () => Activator.CreateInstance(typeof(T3.Gui.OutputUi.ValueOutputUi<>).MakeGenericType(enumType)) as T3.Gui.OutputUi.IOutputUi
                });
            }
        }
    }
}
