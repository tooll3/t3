namespace Lib.image.fx.blur;

[Guid("f634e126-8834-46ea-bd6e-5ebfdc8b0733")]
internal sealed class Bloom : Instance<Bloom>
{

        [Output(Guid = "f3fa372d-f037-48fd-8a8d-a0135b4c20cb")]
        public readonly Slot<T3.Core.DataTypes.Texture2D> Result = new Slot<T3.Core.DataTypes.Texture2D>();

        [Input(Guid = "97d8f330-5957-4309-8c56-d94c1266f6cb")]
        public readonly InputSlot<T3.Core.DataTypes.Texture2D> Image = new InputSlot<T3.Core.DataTypes.Texture2D>();

        [Input(Guid = "28e0b719-0888-4ef6-85d9-bbd75f7a4537")]
        public readonly InputSlot<float> Threshold = new InputSlot<float>();

        [Input(Guid = "bb706662-2555-4f3b-a81e-60e04f052f36")]
        public readonly InputSlot<float> Intensity = new InputSlot<float>();

        [Input(Guid = "c6a0cadc-9e1c-40ac-97f1-d9271b5376df")]
        public readonly InputSlot<float> Blur = new InputSlot<float>();

        [Input(Guid = "2064aba1-658e-4795-a4fc-5be1026d7064")]
        public readonly InputSlot<int> MaxLevels = new InputSlot<int>();

        [Input(Guid = "b6ca6df2-9a48-437a-9bb2-1cb5b358bc47")]
        public readonly InputSlot<float> Shape = new InputSlot<float>();

}