using System.Runtime.InteropServices;
using System;
using System.IO;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace lib.io.file
{
	[Guid("5f71d2f8-98c8-4502-8f40-2ea4a1e18cca")]
    public class ReadFile : Instance<ReadFile>
    {
        [Output(Guid = "d792d3b4-b800-41f1-9d69-6ee55751ad37")]
        public readonly Slot<string> Result = new();

        public ReadFile()
        {
            _fileContents = new Resource<string>(FilePath, TryLoad);
            _fileContents.Changed += UpdateResult;
            Result.UpdateAction = Update;
        }

        private void UpdateResult(object sender, string e)
        {
            Result.Value = e;
        }

        private bool TryLoad(FileResource file, string currentValue, out string newValue, out string failureReason)
        {
            if (!file.TryOpenFileStream(out var stream, out failureReason, FileAccess.Read))
            {
                newValue = null;
                return false;
            }

            try
            {
                using var fileStream = stream;
                using var reader = new StreamReader(fileStream);
                newValue = reader.ReadToEnd();
                return true;
            }
            catch (Exception e)
            {
                failureReason = $"Failed to read file {file.AbsolutePath}:" + e.Message;
                newValue = null;
                return false;
            }
        }

        private void Update(EvaluationContext context)
        {
            if(TriggerUpdate.GetValue(context))
                _fileContents.InvokeChangeEvent();
            
            Result.DirtyFlag.Clear();
        }
        
        
        
        [Input(Guid = "24b7e7e1-fe0b-46be-807e-0afacd4800f9")]
        public readonly InputSlot<string> FilePath = new();
        
        [Input(Guid = "5C6241F7-6A4F-4972-A314-98FD070F91DD")]
        public readonly InputSlot<bool> TriggerUpdate = new();

        private Resource<string> _fileContents;
    }
}