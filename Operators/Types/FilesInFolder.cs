using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_f90fcd0a_eab9_4e2a_b393_e8d3a0380823
{
    public class FilesInFolder : Instance<FilesInFolder>
    {
        [Output(Guid = "99bd5b48-7a28-44a7-91e4-98b33cfda20f")]
        public readonly Slot<List<string>> Files = new Slot<List<string>>();


        public FilesInFolder()
        {
            Files.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if (TriggerUpdate.GetValue(context))
            {
                TriggerUpdate.Value = false;
                TriggerUpdate.TypedInputValue.Value = false;
                TriggerUpdate.DirtyFlag.Invalidate();
                TriggerUpdate.DirtyFlag.Clear();
            }
            
            var folderPath = Folder.GetValue(context);
            var filter = Filter.GetValue(context);
            var filePaths = Directory.Exists(folderPath) 
                              ? Directory.GetFiles(folderPath).ToList() 
                              : new List<string>();

            
            Files.Value = string.IsNullOrEmpty(Filter.Value) 
                              ? filePaths 
                              : filePaths.FindAll(filepath => filepath.Contains(filter)).ToList();
        }

        [Input(Guid = "ca9778e7-072c-4304-9043-eeb2dc4ca5d7")]
        public readonly InputSlot<string> Folder = new InputSlot<string>(".");
        
        [Input(Guid = "8B746651-16A5-4274-85DB-0168D30C86B2")]
        public readonly InputSlot<string> Filter = new InputSlot<string>("*.png");
        
        [Input(Guid = "E14A4AAE-E253-4D14-80EF-A90271CD306A")]
        public readonly InputSlot<bool> TriggerUpdate = new InputSlot<bool>();

    }
}