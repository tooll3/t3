using System.IO;
using Newtonsoft.Json;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;
using Point = T3.Core.DataTypes.Point;

namespace T3.Operators.Types.Id_d5607e3b_15e8_402c_8d54_b29e40415ab0
{
    public class ExportPointList : Instance<ExportPointList>
    {
        [Output(Guid = "ba3d861e-3e22-4cea-9070-b7f53059cf87")]
        public readonly Slot<StructuredList> Result = new Slot<StructuredList>();

        
        public ExportPointList()
        {
            Result.UpdateAction = Update;
            
            
        }

        private bool _triggerSave;

        private void Update(EvaluationContext context)
        {
            var saveTriggered = MathUtils.WasTriggered(TriggerSave.GetValue(context), ref _triggerSave);
            var filepath = FilePath.GetValue(context);
            var inputListValue = InputList.GetValue(context);
            InputList.TypedDefaultValue.Value = inputListValue;

            if (saveTriggered)
            {
                if (inputListValue != null)
                {
                    //var filepath = BuildFilepathForSymbol(symbol, SymbolExtension);

                    using var sw = new StreamWriter(filepath);
                    using var writer = new JsonTextWriter(sw) { Formatting = Formatting.Indented };
                    //SymbolJson.WriteSymbol(symbol, writer);
                    inputListValue.Write(writer);
                }
            }
                
            Result.Value = inputListValue;
        }
        [Input(Guid = "B0DCEB47-4EE5-481F-A310-173C336F4FBE")]
        public readonly InputSlot<bool> TriggerSave = new ();
        
        [Input(Guid = "043E9707-98FE-420C-A546-292459C2FCCA")]
        public readonly InputSlot<string> FilePath = new ();
        
        [Input(Guid = "b3d57d74-ac47-4287-b42a-d85e64501eb5")]
        public readonly InputSlot<StructuredList> InputList = new(new StructuredList<Point>(15));
        
    }
}