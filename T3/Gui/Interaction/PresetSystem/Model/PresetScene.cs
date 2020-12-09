using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;


namespace T3.Gui.Interaction.PresetSystem.Model
{
    public class PresetScene
    {
        public Guid Id = Guid.NewGuid();
        public string Title;

        public void ToJson(JsonTextWriter writer)
        {
            writer.WriteValue("Id", Id);
            writer.WriteObject("Title", Title);
        }

        public static PresetScene FromJson(JToken sceneToken)
        {
            if (!sceneToken.HasValues)
                return null;
            
            return new PresetScene()
                       {
                           Id = Guid.Parse(sceneToken["Id"].Value<string>()),
                           Title = sceneToken.Value<string>("Title"),
                       };
        }
    }
}