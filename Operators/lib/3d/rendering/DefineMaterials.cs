using System.Collections.Generic;
using System.IO;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Rendering.Material;

namespace T3.Operators.Types.Id_0bd77dd6_a93a_4e2e_b69b_bbeb73cb5ae9
{
    public class DefineMaterials : Instance<DefineMaterials>
    {
        [Output(Guid = "9e5f90f2-370c-4aef-8819-c3c5c7e5edc2")]
        public readonly Slot<Command> Output = new();

        public DefineMaterials()
        {
            Output.UpdateAction = Update;
        }
        

        private Dictionary<string, PbrMaterial> _materials = new();
        
        private void Update(EvaluationContext context)
        {
            var materialsInputs = Materials.GetCollectedTypedInputs();

            var addedMaterialsCount = 0;
            var previousMaterialCount = context.Materials.Count;
            
            foreach (var slot in materialsInputs)
            {
                var m = slot.GetValue(context);

                if (m == null)
                    continue;

                addedMaterialsCount++;
                context.Materials.Add(m);
            }
            
            
            // Update subgraph
            SubGraph.GetValue(context);

            if(addedMaterialsCount > 0)
                context.Materials.RemoveRange( previousMaterialCount, addedMaterialsCount );
            
        }


        [Input(Guid = "ab03556d-02a5-4329-94af-58563f93e159")]
        public readonly InputSlot<Command> SubGraph = new();
        
        
        [Input(Guid = "6299A306-D96C-425D-B44C-0340358439CB")]
        public readonly MultiInputSlot<PbrMaterial> Materials = new();

        
    }
}