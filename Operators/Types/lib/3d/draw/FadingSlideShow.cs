using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_cb89ed1d_03ea_4880_bfa0_1dd723e4bdab
{
    public class FadingSlideShow : Instance<FadingSlideShow>
    {
        [Output(Guid = "fd703cd6-ed0a-473b-9620-d5b5f5547774")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Output(Guid = "51ba413f-1a7a-4860-8654-bdbd78a1bba4")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "61469462-04ef-4f75-bd58-2bd42b8da15a")]
        public readonly InputSlot<float> IndexAndFraction = new InputSlot<float>();

        [Input(Guid = "18aeda2b-ff90-4cf4-9665-c8c65a23cb5f")]
        public readonly InputSlot<float> BlendSpeed = new InputSlot<float>();

        [Input(Guid = "c3765d6d-4f45-4425-a86c-1cdd13eff296")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();

        [Input(Guid = "8feb4ec2-acc6-4a46-852b-70c4f156f6de")]
        public readonly InputSlot<System.Numerics.Vector2> Position = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "651d479c-a3d4-4d18-bdcd-2400e2cdb56b")]
        public readonly InputSlot<float> RandomOffset = new InputSlot<float>();

        [Input(Guid = "ca8724d8-a362-4af4-bdf8-a80a1b0ef72e")]
        public readonly InputSlot<string> FolderWithImages = new InputSlot<string>();

        [Input(Guid = "3cb08250-213c-4fa6-b9a0-1c92bf01ad6e")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "cabb74b7-72c3-4916-8096-5890fc8e7023")]
        public readonly InputSlot<System.Numerics.Vector4> BackgroundColor = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "0f7305c9-4372-4e62-82ff-a377bcbffc12")]
        public readonly InputSlot<int> ScaleMode = new InputSlot<int>();

    }
}

