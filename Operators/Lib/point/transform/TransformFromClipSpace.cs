using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.point.transform
{
	[Guid("81377edc-0a42-4bb1-9440-2f2433d5757f")]
    public class TransformFromClipSpace : Instance<TransformFromClipSpace>
    {
        [Output(Guid = "fa70200b-cfcb-4efe-afbd-48cefea1ca39")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> Output = new();

        [Input(Guid = "e02d3e37-4da6-4528-b06f-6f26c818d1d8")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new();
        
        
        
        private enum Spaces
        {
            PointSpace,
            ObjectSpace,
        }
        
        
        
        
        
    }
}

