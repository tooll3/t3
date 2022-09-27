using System;
using System.Collections.Generic;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_845371ef_a5c2_4ca2_8315_ea2b62f63ee2
{
    public class PickMeshBuffer : Instance<PickMeshBuffer>
    {
        [Output(Guid = "2F4733F8-ADF1-4A6D-B207-5EE2D566CAE3")]
        public readonly Slot<T3.Core.DataTypes.MeshBuffers> Output = new Slot<T3.Core.DataTypes.MeshBuffers>();
        
        
        public PickMeshBuffer()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var connections = Input.GetCollectedTypedInputs();
            if (connections == null || connections.Count == 0)
                return;

            var index = Index.GetValue(context);
            if (index < 0)
                index = -index;
            
            index %= connections.Count;
            Output.Value = connections[index].GetValue(context);
        }        
        

        // [Input(Guid = "895a5b7e-d1b5-4779-bff4-d1e7d3d75701")]
        // public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        // [Input(Guid = "025cb23d-7612-4ae3-91d5-b783a65e02d0")]
        // public readonly InputSlot<int> Count = new InputSlot<int>();

        [Input(Guid = "076AFDCC-C9AF-4875-B97A-D8132996B35A")]
        public readonly InputSlot<int> Index = new InputSlot<int>();

        [Input(Guid = "7BB6F999-214A-448A-A7F7-BE447113785E")]
        public readonly MultiInputSlot<MeshBuffers> Input = new MultiInputSlot<MeshBuffers>();
    }
}

