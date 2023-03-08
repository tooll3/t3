using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_5972a57b_73cd_49b2_8b24_96636a4c294b
{
    public class RgbTV : Instance<RgbTV>
    {
        [Output(Guid = "22eac013-881d-486a-8041-5cae32b8dca1")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "2dbfdd5d-8b4b-447c-bd19-326d46657ea1")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "38529a44-4622-4c87-886e-72f4400ec468")]
        public readonly InputSlot<float> PatternAmount = new InputSlot<float>();

        [Input(Guid = "7ba6f801-3d73-4f67-a6bc-40b409f9d116")]
        public readonly InputSlot<float> PatternAspect = new InputSlot<float>();

        [Input(Guid = "866493b5-50ec-4587-861f-87d5d3a698e5")]
        public readonly InputSlot<float> PatternSize = new InputSlot<float>();

        [Input(Guid = "be5ee091-ceea-4b69-8e81-427c8696cdeb")]
        public readonly InputSlot<float> ShiftColumns = new InputSlot<float>();

        [Input(Guid = "f58da234-b925-40ca-b449-1ac882bd2e96")]
        public readonly InputSlot<float> Gaps = new InputSlot<float>();

        [Input(Guid = "9b3a821b-3bad-4411-99ba-669436e240fd")]
        public readonly InputSlot<System.Numerics.Vector2> PatternOffset = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "e3e3f393-0c43-4c71-b134-adc094ca2965")]
        public readonly InputSlot<float> Buldge = new InputSlot<float>();

        [Input(Guid = "5f536f1f-49da-435d-a02b-002a3673c240")]
        public readonly InputSlot<System.Numerics.Vector2> PatternBlur = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "b03dd009-99b7-4846-8d81-9c67f57fbd91")]
        public readonly InputSlot<float> GlitchTime = new InputSlot<float>();

        [Input(Guid = "f76c6202-34dc-4c10-adab-c10cb7665fed")]
        public readonly InputSlot<float> GlitchAmount = new InputSlot<float>();

        [Input(Guid = "9964e5dc-071a-4dce-bca4-994fbda19906")]
        public readonly InputSlot<float> GlitchVignette = new InputSlot<float>();

        [Input(Guid = "13bceb18-aa27-480a-bf34-c93c758dabca")]
        public readonly InputSlot<float> GlitchOffset = new InputSlot<float>();

        [Input(Guid = "5cb6a27b-db0d-4d85-83aa-87316ba6f5a0")]
        public readonly InputSlot<float> GlitchFlicker = new InputSlot<float>();

        [Input(Guid = "a13b757c-62ed-478b-b0fe-70cceb43586e")]
        public readonly InputSlot<float> Noise = new InputSlot<float>();

        [Input(Guid = "06781f30-4be8-4eb6-95fd-7e8b16081a64")]
        public readonly InputSlot<float> NoiseColorize = new InputSlot<float>();

        [Input(Guid = "86233c09-0ca4-4393-9480-b8275a5bd9e8")]
        public readonly InputSlot<float> BlurBackdrop = new InputSlot<float>();

        [Input(Guid = "be2bf88f-870e-4dd9-a0a7-e10e38b9a8d1")]
        public readonly InputSlot<float> OverdrawSteps = new InputSlot<float>();

        [Input(Guid = "c96bcad4-c73e-4731-9aa0-16dfaa91ba57")]
        public readonly InputSlot<float> OverdrawScale = new InputSlot<float>();

        [Input(Guid = "f8358e13-a769-45e9-b345-f172fabd9c74")]
        public readonly InputSlot<System.Numerics.Vector2> OverdrawOffset = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "fc4e5ca3-edb3-4ebc-8210-b339d7914741")]
        public readonly InputSlot<float> BlurImage = new InputSlot<float>();

        [Input(Guid = "1d2328ce-d0c2-4b0b-9318-4a7c5dcfd411")]
        public readonly InputSlot<float> P1 = new InputSlot<float>();

        [Input(Guid = "95c2f7b3-52e9-4d22-bf1f-c2ad2d0fb8da")]
        public readonly InputSlot<float> P2 = new InputSlot<float>();

        [Input(Guid = "6d4c28fd-2bea-48a6-a87a-df0b46f76d7c")]
        public readonly InputSlot<float> P3 = new InputSlot<float>();

        [Input(Guid = "ac6c18b6-8896-43e7-b063-00cd67fb3545")]
        public readonly InputSlot<float> P4 = new InputSlot<float>();

        [Input(Guid = "e3f06019-97a4-44b3-b9b4-36d07cbd53ac")]
        public readonly InputSlot<SharpDX.Size2> Resolution = new InputSlot<SharpDX.Size2>();
    }
}

