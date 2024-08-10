using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_76cd7578_0f97_49a6_938a_caeaa98deaac
{
    public class _DrawMeshChunksAtPoints : Instance<_DrawMeshChunksAtPoints>
    {
        [Output(Guid = "95dee492-83bc-4b09-97bf-a8c7d20123ac")]
        public readonly Slot<T3.Core.DataTypes.Command> Update = new ();
        
        [Output(Guid = "d331c9b8-500b-4a1d-a68d-63819210ec1c")]
        public readonly Slot<SharpDX.Direct3D11.ShaderResourceView> CellPointIndices = new();

        [Output(Guid = "47d78d09-2175-4e14-9774-3b0152937de0")]
        public readonly Slot<SharpDX.Direct3D11.ShaderResourceView> PointCellIndices = new();

        [Output(Guid = "bacec266-7c41-4eda-8b96-8f590a14ee59")]
        public readonly Slot<SharpDX.Direct3D11.ShaderResourceView> HashGridCells = new();

        [Output(Guid = "b01da436-228a-4b1e-a28d-c352820fa2ec")]
        public readonly Slot<SharpDX.Direct3D11.ShaderResourceView> CellPointCounts = new();

        [Output(Guid = "f85678fe-6c0d-41b6-989c-ecd2e8b06681")]
        public readonly Slot<SharpDX.Direct3D11.ShaderResourceView> CellRangeIndices = new();



        [Input(Guid = "e899e468-112a-4be7-b4ca-98bc944cb37b")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> PointsA_ = new();

        [Input(Guid = "eb4fd871-0fcd-4f5f-926c-c7584b9fb85b")]
        public readonly InputSlot<float> CellSize = new();

        [Input(Guid = "cd9722fe-6d0c-4caf-911c-ef3367374f85")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> ChunkIndices = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "d1747719-e998-4dfb-92f1-8bbdbc55699c")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> ChunkDefinitions = new InputSlot<T3.Core.DataTypes.BufferWithViews>();
    }
}

