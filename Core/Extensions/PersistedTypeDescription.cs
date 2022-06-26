using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using T3.Core.Operator;

namespace T3.Core.Extensions
{
    public struct PersistedTypeDescription
    {
        public Type Type;
        public string TypeName;
        public Func<InputValue> DefaultValueCreator;
        public Action<JsonTextWriter, object> ValueToJsonConverter;
        public Func<JToken, object> JsonToValueConverter;

        public PersistedTypeDescription(Type type, 
            string typeName, 
            Func<InputValue> defaultValueCreator, 
            Action<JsonTextWriter, object> valueToJsonConverter,
            Func<JToken, object> jsonToValueConverter)
        {
            Type = type;
            TypeName = typeName;
            DefaultValueCreator = defaultValueCreator;
            ValueToJsonConverter = valueToJsonConverter;
            JsonToValueConverter = jsonToValueConverter;
        }

    }
}
