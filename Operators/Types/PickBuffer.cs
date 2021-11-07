using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_e6bbbeef_08d8_4105_b84d_39edadb549c0
{
    public class PickBuffer : Instance<PickBuffer>
    {
        [Output(Guid = "32D2645B-B627-437A-AFEC-7E728E2B54F5")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> Output = new Slot<T3.Core.DataTypes.BufferWithViews>();
        
        
        public PickBuffer()
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
            //Log.Debug($"Fetching buffer with index {index}");
            Output.Value = connections[index].GetValue(context);
        }        
        

        // [Input(Guid = "895a5b7e-d1b5-4779-bff4-d1e7d3d75701")]
        // public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        // [Input(Guid = "025cb23d-7612-4ae3-91d5-b783a65e02d0")]
        // public readonly InputSlot<int> Count = new InputSlot<int>();

        [Input(Guid = "04776dc8-7b84-41f5-973c-22cadbf44f02")]
        public readonly InputSlot<int> Index = new InputSlot<int>();

        [Input(Guid = "6B1C6232-819A-4021-82A9-994F8928BE13")]
        public readonly MultiInputSlot<BufferWithViews> Input = new MultiInputSlot<BufferWithViews>();
    }
}

