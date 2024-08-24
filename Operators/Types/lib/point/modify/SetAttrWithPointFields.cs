using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_21b0a346_f214_449f_ae27_7bfbe5395d66
{
    public class SetAttrWithPointFields : Instance<SetAttrWithPointFields>
    {

        [Output(Guid = "1e4de159-c526-44b4-9d31-e1aec95b9bad")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> Output = new();

        [Input(Guid = "cfc0c91e-4cb6-4408-a463-21209d7d4742")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "df08c590-c398-4b92-aa09-ab57abc42aaf")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> FieldPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "7485a1bc-0983-4348-a7d7-d723e92eecd9")]
        public readonly InputSlot<float> Amount = new InputSlot<float>();

        [Input(Guid = "d87f1661-957a-4b27-a915-ad0d5a526143")]
        public readonly InputSlot<float> ColorDistance = new InputSlot<float>();

        [Input(Guid = "11584b2f-3df4-4073-bcde-01e0fdaa3f5d")]
        public readonly InputSlot<float> Range = new InputSlot<float>();

        [Input(Guid = "ea732975-c8c7-4210-b2ea-0e73298c54d1")]
        public readonly InputSlot<float> Phase = new InputSlot<float>();

        [Input(Guid = "d3d92fe5-9a56-45f6-acc7-67370d744c0e")]
        public readonly InputSlot<T3.Core.DataTypes.Curve> WCurve = new InputSlot<T3.Core.DataTypes.Curve>();

        [Input(Guid = "959d8403-74d0-4f8b-b2f8-28ba0d26a3e6")]
        public readonly InputSlot<T3.Core.DataTypes.Gradient> Gradient = new InputSlot<T3.Core.DataTypes.Gradient>();

        private enum SpreadModes
        {
            UseBufferOrder,
            UseW,
        }
        

        private enum MappingModes
        {
            Normal,
            ForStart,
            PingPong,
            Repeat,
            UseOriginalW,
        }
        
        private enum Modes
        {
            Replace,
            Multiply,
            Add,
        }
    }
}

