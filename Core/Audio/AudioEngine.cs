using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;
using T3.Core.Logging;

namespace Core.Audio
{
    /// <summary>
    /// Controls loading, playback and discarding or audio clips.
    /// </summary>
    public class AudioEngine
    {
        
    }

    public class AudioClip
    {
        public Guid Id;
        public string FilePath;
        public double StartTime;
        public double EndTime;
        public double Bpm = 120;
        public bool DiscardAfterUse = true;
        
        
        #region serialization
        public static AudioClip FromJson(JToken jToken)
        {
            var idToken = jToken[nameof(Id)];

            var idString = idToken?.Value<string>();
            if (idString == null)
                return null;

            var newAudioClip = new AudioClip
                                   {
                                       Id = Guid.Parse(idString),
                                       FilePath = jToken[nameof(FilePath)]?.Value<string>() ?? String.Empty,
                                       StartTime = jToken[nameof(StartTime)]?.Value<double>() ?? 0,
                                       EndTime = jToken[nameof(EndTime)]?.Value<double>() ?? 0,
                                       Bpm = jToken[nameof(Bpm)]?.Value<double>() ?? 0,
                                       DiscardAfterUse = jToken[nameof(DiscardAfterUse)]?.Value<bool>() ?? true,
                                   };
            
            return newAudioClip;
        }

        public void ToJson(JsonTextWriter writer)
        {
            //writer.WritePropertyName(Id.ToString());
            writer.WriteStartObject();
            {
                writer.WriteValue(nameof(Id), Id);
                writer.WriteValue(nameof(StartTime), StartTime);
                writer.WriteValue(nameof(EndTime), EndTime);
                writer.WriteValue(nameof(Bpm), Bpm);
                writer.WriteValue(nameof(DiscardAfterUse), DiscardAfterUse);
                writer.WriteObject(nameof(FilePath), FilePath);
            }
            writer.WriteEndObject();
        }

        #endregion        
    }
}