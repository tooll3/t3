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

        public static void ToJson(GroupParameter parameter, JsonTextWriter writer)
        {
            writer.WriteStartObject();
            if (parameter != null)
            {
                writer.WriteValue("Id", parameter.Id);
                writer.WriteObject("SymbolChildId", parameter.SymbolChildId);
                writer.WriteObject("InputId", parameter.InputId);
                writer.WriteObject("Title", parameter.Title);
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