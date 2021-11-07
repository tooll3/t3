using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_5b9f1d97_4e10_4f31_ba83_4cbf7be9719b
{
    public class MeshBufferComponents : Instance<MeshBufferComponents>
    {
        [Output(Guid = "0C5E2EC1-AB60-43CE-B823-3DF096FF9A28")]
        public readonly Slot<BufferWithViews> Vertices = new Slot<BufferWithViews>();

        [Output(Guid = "78C53086-BB28-4C58-8B51-42CFDF6620C4")]
        public readonly Slot<BufferWithViews> Indices = new Slot<BufferWithViews>();

        public MeshBufferComponents()
        {
            Vertices.UpdateAction = Update;
            Indices.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var mesh = MeshBuffers.GetValue(context);
            if (mesh == null)
            {
                Vertices.Value = null;
                Indices.Value = null;
                return;
            }

            Vertices.Value = mesh.VertexBuffer;
            Indices.Value = mesh.IndicesBuffer;
            
            Vertices.DirtyFlag.Clear();
            Indices.DirtyFlag.Clear();
        }

        [Input(Guid = "1B0B7587-DE86-4FC4-BE78-A21392E8AA9B")]
        public readonly InputSlot<MeshBuffers> MeshBuffers = new InputSlot<MeshBuffers>();
    }
}