using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_13d23535_dfa5_458e_93e3_158d83e0188b
{
    public class PointCloudSlicer : Instance<PointCloudSlicer>
    {
        [Output(Guid = "e8993426-0ce0-4ec2-b335-fa28e568db16")]
        public readonly Slot<T3.Core.Command> Command = new Slot<T3.Core.Command>();

        [Output(Guid = "fb114e91-5b5f-4112-a447-8a1e72582672")]
        public readonly Slot<SharpDX.Direct3D11.ShaderResourceView> SlicedData = new Slot<SharpDX.Direct3D11.ShaderResourceView>();

        [Output(Guid = "0fbbaf49-f06e-4f50-a955-4c01a7ad3dea")]
        public readonly Slot<SharpDX.Direct3D11.Buffer> Buffer = new Slot<SharpDX.Direct3D11.Buffer>();

        [Input(Guid = "cad42998-190e-467a-b882-8b805fc693e3")]
        public readonly InputSlot<SharpDX.Direct3D11.ShaderResourceView> Data = new InputSlot<SharpDX.Direct3D11.ShaderResourceView>();

        [Input(Guid = "da7abd8c-9bdb-47ff-9171-543ea3d490ab")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();

        [Input(Guid = "1873641d-e1f1-4c63-b88c-b19d271abb14")]
        public readonly InputSlot<float> Range = new InputSlot<float>();
    }
}

