using System;
using System.IO;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_5f71d2f8_98c8_4502_8f40_2ea4a1e18cca
{
    public class ReadFile : Instance<ReadFile>
    {
        [Output(Guid = "d792d3b4-b800-41f1-9d69-6ee55751ad37")]
        public readonly Slot<string> Result = new();

        public ReadFile()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var triggerUpdate = TriggerUpdate.GetValue(context);
            
            var filepath = FilePath.GetValue(context);
            ResourceFileWatcher.AddFileHook(filepath, () => {FilePath.DirtyFlag.Invalidate();});
            
            //ResourceManager.Instance().

            try
            {
                Result.Value = File.ReadAllText(filepath);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to read file {filepath}:" + e.Message);
            }
        }
        
        [Input(Guid = "24b7e7e1-fe0b-46be-807e-0afacd4800f9")]
        public readonly InputSlot<string> FilePath = new();
        
        [Input(Guid = "5C6241F7-6A4F-4972-A314-98FD070F91DD")]
        public readonly InputSlot<bool> TriggerUpdate = new();
    }
}