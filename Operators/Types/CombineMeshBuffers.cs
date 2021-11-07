using SharpDX;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_e0849edd_ea1b_4657_b22d_5aa646318aa8
{
    public class CombineMeshBuffers : Instance<CombineMeshBuffers>
    {
        [Output(Guid = "D71893DD-6CA2-4AB7-9E04-0BD7285ECCFB")]
        public readonly Slot<MeshBuffers> MeshBuffers = new Slot<MeshBuffers>();
        
        
        public CombineMeshBuffers()
        {
            MeshBuffers.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if (PrepareCommand.IsConnected)
            {
                PrepareCommand.GetValue(context);
            }
            
            var vertices = Vertices.GetValue(context);
            var indices = Indices.GetValue(context);
            
            if (vertices == null || indices == null)
            {
                MeshBuffers.Value = null;
                return;
            }

            _result.VertexBuffer = vertices;
            _result.IndicesBuffer = indices;
            MeshBuffers.Value = _result;
        }

        private MeshBuffers _result = new MeshBuffers();

        [Input(Guid = "5E82E351-E8A8-4594-83E3-E86C888D0588")]
        public readonly InputSlot<Command> PrepareCommand = new InputSlot<Command>();
        
        [Input(Guid = "BA53B274-62CA-40A2-B8D2-87D08F0BC259")]
        public readonly InputSlot<BufferWithViews> Vertices = new InputSlot<BufferWithViews>();
        
        [Input(Guid = "892838C5-FA5A-418E-81D6-A3A523819324")]
        public readonly InputSlot<BufferWithViews> Indices = new InputSlot<BufferWithViews>();
    }
}