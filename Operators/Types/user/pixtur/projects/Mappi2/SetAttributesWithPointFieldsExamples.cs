using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace Types.user.pixtur.projects.Mappi2
{
    [Guid("7644e221-9785-414b-905d-31fd68ec080c")]
    public class SetAttributesWithPointFieldsExamples : Instance<SetAttributesWithPointFieldsExamples>
    {

        [Output(Guid = "c48f6792-915b-47d4-a880-3adbad88b6fd")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();


    }
}

