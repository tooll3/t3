using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;

namespace T3.Gui.Interaction.PresetSystem.Model
{
    public class GroupParameter
    {
        public Guid Id = Guid.NewGuid();
        public string Title;
        public Guid SymbolChildId;

        public Guid InputId;
        // public int ComponentIndex;    // for handling Vector inputs
        // public Type InputType;

        public static void ToJson(GroupParameter obj, JsonTextWriter writer)
        {
            writer.WriteStartObject();
            if (obj != null)
            {
                writer.WriteValue("Id", obj.Id);
                writer.WriteObject("SymbolChildId", obj.SymbolChildId);
                writer.WriteObject("InputId", obj.InputId);
            }
            writer.WriteEndObject();
        }

        public static GroupParameter FromJson(JToken parameterToken)
        {
            if (!parameterToken.HasValues)
                return null;

            var newParameter = new GroupParameter()
                                   {
                                       Id = Guid.Parse(parameterToken["Id"].Value<string>()),
                                       SymbolChildId = Guid.Parse(parameterToken["SymbolChildId"].Value<string>()),
                                       InputId = Guid.Parse(parameterToken["InputId"].Value<string>()),
                                       Title = parameterToken.Value<string>("Title"),
                                   };
            return newParameter;
        }
    }
}