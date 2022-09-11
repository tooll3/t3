using System.Collections.Generic;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using SharpDX.Direct3D11;

namespace T3.Operators.Types.Id_e6070817_cf2e_4430_87e0_bf3dd15afdb5
{
    public class PickTexture : Instance<PickTexture>
    {
        [Output(Guid = "D2F29AC9-EC9E-43AB-8F3F-2C4CD7FC9444")]
        public readonly Slot<Texture2D> Selected = new Slot<Texture2D>();

        public PickTexture()
        {
            Selected.UpdateAction = Update;
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
            Selected.Value = connections[index].GetValue(context);
        }

        [Input(Guid = "6C935163-1729-4DF0-A981-610B4AA7C6A3")]
        public readonly MultiInputSlot<Texture2D> Input = new MultiInputSlot<Texture2D>();

        [Input(Guid = "29e289be-e735-4dd4-8826-5e434cc995fa")]
        public readonly InputSlot<int> Index = new InputSlot<int>(0);
    }
}