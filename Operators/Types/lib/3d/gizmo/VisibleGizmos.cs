using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_d61d7192_9ca3_494e_91e2_10a530ee9375
{
    public class VisibleGizmos : Instance<VisibleGizmos>
    {
        [Output(Guid = "6c29ce06-0512-4815-bc83-ab2e095c0455")]
        public readonly Slot<Command> Output = new Slot<Command>();

        public VisibleGizmos()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var visibility = Visibility.GetValue(context);
            if (visibility == T3.Core.Operator.GizmoVisibility.Inherit)
                visibility = context.ShowGizmos;

            if (visibility != T3.Core.Operator.GizmoVisibility.On)
                return;
            
            
            var commands = Commands.GetCollectedTypedInputs();
            if (commands.Count == 0)
            {
                return;
            }
            
            foreach (var t in commands)
            {
                t.GetValue(context);
            }
        }

        
        [Input(Guid = "4F52683C-F2AA-4D3F-A964-F5232FA98872")]
        public readonly InputSlot<T3.Core.Operator.GizmoVisibility> Visibility = new InputSlot<T3.Core.Operator.GizmoVisibility>();
        
        [Input(Guid = "4d663aa5-e2d4-40e0-8901-abe09cb832c3")]
        public readonly MultiInputSlot<Command> Commands = new MultiInputSlot<Command>();
    }
}