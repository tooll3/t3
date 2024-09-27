namespace lib.math.int2
{
	[Guid("c6d50423-54ea-4c9d-b547-eb78cc2c950c")]
    public class ScaleSize : Instance<ScaleSize>
    {
        [Output(Guid = "c2c27def-70f2-4f07-9796-11b62e5329e2")]
        public readonly Slot<Int2> Result = new();
        
        
        public ScaleSize()
        {
            Result.UpdateAction += Update;
        }

        private void Update(EvaluationContext context)
        {
            var source = InputSize.GetValue(context);
            var factor = Factor.GetValue(context);
            
            Result.Value = new Int2((int)(source.Width * factor), (int)(source.Height * factor));
        }
        
        [Input(Guid = "DDCEB7DF-1C6F-4545-9669-B1B4A80E75E8")]
        public readonly InputSlot<Int2> InputSize = new();
        
        [Input(Guid = "133BBC5A-BDBF-4993-BD1A-878EC93EE04F")]
        public readonly InputSlot<float> Factor = new();
        
    }
}
