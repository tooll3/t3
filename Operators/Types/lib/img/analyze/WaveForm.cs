using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_40528e34_732e_4653_bbcf_eeea36c3c4cf
{
    public class WaveForm : Instance<WaveForm>
    {
        [Output(Guid = "9c1507df-de24-41ac-a8aa-9394f846e646")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output = new Slot<SharpDX.Direct3D11.Texture2D>();


        [Input(Guid = "08206117-24fa-4900-b095-acd3a5ddd58d")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture2d = new InputSlot<SharpDX.Direct3D11.Texture2D>();



        [Input(Guid = "a8861fc0-2cf4-47b9-aede-21afd4a48317")]
        public readonly InputSlot<float> UpperLimit = new InputSlot<float>();

        [Input(Guid = "5ffeb44a-2715-4a9d-bdd9-31df97199b2d")]
        public readonly InputSlot<int> SampleCount = new InputSlot<int>();

        [Input(Guid = "9cd3250b-8bb6-4071-a5d6-f698833391a8")]
        public readonly InputSlot<float> Original = new InputSlot<float>();

        [Input(Guid = "043b949c-a666-4fe0-bbe8-65d627f27474")]
        public readonly InputSlot<float> RGB = new InputSlot<float>();

        [Input(Guid = "abba167e-00b4-439f-b859-a6c3b1e1b976")]
        public readonly InputSlot<float> Grayscale = new InputSlot<float>();

        [Input(Guid = "5d15b643-c350-4288-bb7b-a69ff43e47dc")]
        public readonly InputSlot<float> Lines = new InputSlot<float>();
    }
}

