using System.Numerics;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils.Geometry;
using Vector4 = System.Numerics.Vector4;

namespace T3.Operators.Types.Id_195afff5_13f6_4c5d_af49_655a4f92c2f8
{
    public class ApplyTransformMatrix : Instance<ApplyTransformMatrix>
    {
        [Output(Guid = "51334471-d9fe-4574-8541-f87b67f2deab")]
        public readonly Slot<Command> Output = new();

        public ApplyTransformMatrix()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var matrix = TransformRows.GetValue(context).ToMatrixFromRows();
            matrix.Transpose();
            
            var previousObjectToWorld = context.ObjectToWorld;
            context.ObjectToWorld = Matrix4x4.Multiply(matrix, context.ObjectToWorld);
            Command.GetValue(context);
            context.ObjectToWorld = previousObjectToWorld;

            
            // var previousWorldTobject = context.ObjectToWorld;
            //
            // context.ObjectToWorld = Matrix.Multiply(, context.ObjectToWorld);
            //
            // Command.GetValue(context);
            // context.ObjectToWorld = previousWorldTobject;
        }

        [Input(Guid = "f7d28833-d894-446f-9402-e8ac74794870")]
        public readonly InputSlot<Command> Command = new();

        [Input(Guid = "c3b1ba6c-4306-4ae4-9429-d1f2461e2e8c")]
        public readonly InputSlot<Vector4[]> TransformRows = new();
        
    }
}