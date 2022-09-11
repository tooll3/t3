using T3.Core;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_4315fe6a_dacd_4eef_814d_256edffb29ff
{
    public class ReactionDiffusion : Instance<ReactionDiffusion>
    {
        [Output(Guid = "27ce53fb-df27-4cd1-b1f1-9cf715d938ff")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();

        [Input(Guid = "bc39e2bc-cfb3-48db-af84-ac3b3c7390ca")]
        public readonly InputSlot<float> DiffusionA = new InputSlot<float>();

        [Input(Guid = "91cb0734-1d12-4e48-aa1d-3c7513d9d2ad")]
        public readonly InputSlot<float> DiffusionB = new InputSlot<float>();

        [Input(Guid = "eeea17a9-0720-4381-93ec-1b308511c9ac")]
        public readonly InputSlot<float> FeedRate = new InputSlot<float>();

        [Input(Guid = "6392409f-7850-45f4-bd2b-6cf520e1ef69")]
        public readonly InputSlot<float> KillRate = new InputSlot<float>();

        [Input(Guid = "df77f779-28c7-4a79-85b4-8c9cedc68236")]
        public readonly InputSlot<float> ReactionSpeed = new InputSlot<float>();

        [Input(Guid = "fd68e8b8-e773-45c9-a325-67d239cb9859")]
        public readonly MultiInputSlot<T3.Core.Command> Command = new MultiInputSlot<T3.Core.Command>();

        [Input(Guid = "28469baf-2c2f-46a6-8c88-c5e3a37ba5fc")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> FxTexture = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "5a84aa26-d0ae-4b6c-856e-a1c9b88e141a")]
        public readonly InputSlot<float> RFx_KillWeight = new InputSlot<float>();

        [Input(Guid = "910fef53-77f0-4e5b-baf0-b6e69700978d")]
        public readonly InputSlot<float> GFx_FeedWeight = new InputSlot<float>();

        [Input(Guid = "ddb4bf9b-6f81-471b-869e-ee57c2f926e0")]
        public readonly InputSlot<float> BFx_FillWeight = new InputSlot<float>();

        [Input(Guid = "4cb7676d-9698-4984-a6ae-42712babfe36")]
        public readonly InputSlot<float> Zoom = new InputSlot<float>();

        [Input(Guid = "ce2a8c7b-d9ba-4189-8067-066d5f325ef0")]
        public readonly InputSlot<float> Iterations = new InputSlot<float>();

    }
}

