using System;
using System.Linq;
using SharpDX;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Buffer = SharpDX.Direct3D11.Buffer;
using Point = T3.Core.DataTypes.Point;

namespace T3.Operators.Types.Id_d5607e3b_15e8_402c_8d54_b29e40415ab0
{
    public class PointList : Instance<PointList>
    {
        [Output(Guid = "ba3d861e-3e22-4cea-9070-b7f53059cf87")]
        public readonly Slot<StructuredList> Result = new Slot<StructuredList>();

        
        public PointList()
        {
            Result.UpdateAction = Update;
        }


        private void Update(EvaluationContext context)
        {
            InputList.TypedDefaultValue.Value = InputList.Value;
            Result.Value = InputList.GetValue(context);
        }


        [Input(Guid = "b3d57d74-ac47-4287-b42a-d85e64501eb5")]
        public readonly InputSlot<StructuredList> InputList = new InputSlot<StructuredList>(new StructuredList<Point>(15));
    }
}