using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Gui.Interaction.PresetSystem.Model;

using T3.Core;

namespace T3.Gui.Interaction.PresetSystem
{
    public class ParameterGroup
    {
        public Guid Id = Guid.NewGuid();
        public string Title;
        public List<GroupParameter> Parameters = new List<GroupParameter>(16);

        // TODO: Do not serialize
        public Preset ActivePreset { get; set; }
        public List<Preset> BlendedPresets { get; set; } = new List<Preset>();

        public GroupParameter AddParameterToIndex(GroupParameter parameter, int index)
        {
            // Extend list
            while (Parameters.Count <= index)
            {
                Parameters.Add(null);
            }

            Parameters[index] = parameter;
            return parameter;
        }

        public void SetActivePreset(Preset preset)
        {
            if (ActivePreset != null)
                ActivePreset.State = Preset.States.InActive;
            
            StopBlending();

            ActivePreset = preset;
            if(preset !=null)
                preset.State = Preset.States.Active;
        }

        public void StopBlending()
        {
            foreach (var p in BlendedPresets)
            {
                p.State = Preset.States.InActive;
            }
            BlendedPresets.Clear();
        }
        
        public void ToJson(JsonTextWriter writer)
        {
            writer.WriteValue("Id", Id);
            writer.WriteObject("Title", Title);
            
            // TODO: Implement PresetSystem.FindPresetInGroup(group,preset);
            // if (ActivePreset != null)
            // {
            //     writer.WritePropertyName("ActivePresetAddress");
            //     
            // }
            
            writer.WritePropertyName("Parameters");
            writer.WriteStartArray();
            foreach (var param in Parameters)
            {
                GroupParameter.ToJson(param, writer);
            }
            writer.WriteEndArray();
        }

        public static ParameterGroup FromJson(JToken groupToken)
        {
            if (!groupToken.HasValues)
                return null;
            
            var newGroup = new ParameterGroup()
                               {
                                   Id = Guid.Parse(groupToken["Id"].Value<string>()),
                                   Title = groupToken.Value<string>("Title"),
                               };
            
            foreach (var parameterToken in (JArray)groupToken["Parameters"])
            {
                newGroup.Parameters.Add(!parameterToken.HasValues ? null : GroupParameter.FromJson(parameterToken));
            }

            return newGroup;
            //Guid parameterId = Guid.Parse(presetToken["ParameterId"].Value<string>());
        }


    }
}