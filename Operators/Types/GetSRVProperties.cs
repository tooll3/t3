using System;
using SharpDX;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types.Id_bc489196_9a30_4580_af6f_dc059f226da1
{
    public class GetSRVProperties : Instance<GetSRVProperties>
    {
        [Output(Guid = "431B39FD-4B62-478B-BBFA-4346102C3F61")]
        public readonly Slot<int> ElementCount = new Slot<int>();

        [Output(Guid = "59C4FE70-9129-4BCE-BA39-6D252A59FB97")]
        public readonly Slot<Buffer> Buffer = new Slot<Buffer>();

        public GetSRVProperties()
        {
            ElementCount.UpdateAction = Update;
            Buffer.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var srv = SRV.GetValue(context);
            if (srv == null)
                return;

            try
            {
                ElementCount.Value = srv.Description.Buffer.ElementCount;
            }
            catch (Exception e)
            {
                Log.Error("Failed to get SRVProperties: " + e.Message, SymbolChildId);
            }
        }

        [Input(Guid = "E79473F4-3FD2-467E-ACDA-B27EF7DAE6A9")]
        public readonly InputSlot<ShaderResourceView> SRV = new InputSlot<ShaderResourceView>();
    }
}