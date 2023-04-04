using System;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_3246cf5a_3c9b_4765_89d1_68852a3dd7a1
{
    public class SaveParameterSet : Instance<SaveParameterSet>
    {
        [Output(Guid = "a75a50ac-9361-4cac-8208-3c79f329bad2")]
        public readonly Slot<string> Result = new();

        public SaveParameterSet()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var wasTriggered = MathUtils.WasTriggered(Trigger.GetValue(context), ref _trigger);

            var position = Position.GetValue(context);
            var newSet = new ParameterSet
                             {
                                 X = position.X,
                                 Y = position.Y,
                                 SceneIndex = SceneIndex.GetValue(context),
                                 UserHash = 0
                             };
            if(wasTriggered)
                SetRequestAsync(newSet);
        }

        private async void SetRequestAsync(ParameterSet parameterSet)
        {
            try
            {
                var client = new HttpClient(new HttpClientHandler() { UseDefaultCredentials = true });
                
                
                var request = new HttpRequestMessage
                                  {
                                      RequestUri = new Uri("https://oyjobypyozkufrbrmqnt.supabase.co/rest/v1/scores"),
                                      Method = HttpMethod.Post,
                                  };
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Add("apikey",
                                    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im95am9ieXB5b3prdWZyYnJtcW50Iiwicm9sZSI6ImFub24iLCJpYXQiOjE2ODAwMDA4MTUsImV4cCI6MTk5NTU3NjgxNX0.9Km2SmPnOQre_obeKr_DSr2l33PECtMWIF1VuxE1zck");
                request.Headers.Add("Authorization",
                                    "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im95am9ieXB5b3prdWZyYnJtcW50Iiwicm9sZSI6ImFub24iLCJpYXQiOjE2ODAwMDA4MTUsImV4cCI6MTk5NTU3NjgxNX0.9Km2SmPnOQre_obeKr_DSr2l33PECtMWIF1VuxE1zck");
                request.Headers.Add("Prefer","return=minimal");
                //var content = new StringContent("{'data': {'foo':'bar2'}}", Encoding.UTF8, "application/json");

                var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(parameterSet);
                var s = "{\"data\":" + jsonString + "}";
                Log.Debug("data:" + s);
                //var s2 = "{\"data\":{\"foo\":\"bar\"}}";
                var content = new StringContent(s, Encoding.UTF8, "application/json");
                request.Content = content;
                
                var httpTask = await client.SendAsync(request);
                Result.Value =await httpTask.Content.ReadAsStringAsync();;
                Result.DirtyFlag.Invalidate();
            }
            catch (Exception e)
            {
                Log.Warning("Download failed: " + e.Message);
            }
        }

        private bool _trigger;

        private class ParameterSet
        {
            public float X;
            public float Y;
            public int SceneIndex;
            public int UserHash;
        }
        
        
        [Input(Guid = "D6A017AF-DF4E-4532-8A4A-208DF224D60A")]
        public readonly InputSlot<bool> Trigger = new();


        
        [Input(Guid = "BDF7D9AB-0E73-4323-816D-AF5CA0A60989")]
        public readonly InputSlot<Vector2> Position = new();
        
        [Input(Guid = "4AA59A00-B56E-4634-AD96-A72EAC3EB859")]
        public readonly InputSlot<int> SceneIndex = new();

        [Input(Guid = "A01BF21F-7673-4D4A-AB65-C2333BD4C8DA")]
        public readonly InputSlot<int> TimeStampInSeconds = new();
        
        [Input(Guid = "BE977A53-7B32-4EB7-B23A-338B0D1C7B6E")]
        public readonly InputSlot<int> UserHash = new();
    }
}