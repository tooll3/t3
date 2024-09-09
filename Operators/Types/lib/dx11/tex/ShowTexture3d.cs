using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_9733f5e1_4514_46de_9e7c_bd3912932d1b
{
    public class ShowTexture3d : Instance<ShowTexture3d>
    {
        [Output(Guid = "f5d05816-108d-4acf-a1f8-e1fbbfac2adb")]
        public readonly Slot<Core.DataTypes.Texture3dWithViews> TextureOutput = new();

        public ShowTexture3d()
        {
            TextureOutput.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Command.GetValue(context);
            TextureOutput.Value = Texture.GetValue(context);
        }

        [Input(Guid = "bf5321e1-56b5-49ee-bd83-7c949bafef16")]
        public readonly InputSlot<Command> Command = new();
        [Input(Guid = "59cb775e-6dc5-4228-88c9-0ba11439cf56")]
        public readonly InputSlot<Core.DataTypes.Texture3dWithViews> Texture = new();
    }
}