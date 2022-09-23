using System;
using System.Collections.Generic;
using System.Collections.Generic;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_cb89ed1d_03ea_4880_bfa0_1dd723e4bdab
{
    public class FadingSlideShow : Instance<FadingSlideShow>
    {
        [Output(Guid = "fd703cd6-ed0a-473b-9620-d5b5f5547774")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "0e3011f8-ae19-41b2-a5df-62e092be57ca")]
        public readonly InputSlot<List<string>> Input = new InputSlot<List<string>>();

        [Input(Guid = "c3765d6d-4f45-4425-a86c-1cdd13eff296")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();

        [Input(Guid = "61469462-04ef-4f75-bd58-2bd42b8da15a")]
        public readonly InputSlot<float> IndexAndFraction = new InputSlot<float>();

        [Input(Guid = "18aeda2b-ff90-4cf4-9665-c8c65a23cb5f")]
        public readonly InputSlot<float> BlendSpeed = new InputSlot<float>();

    }
}

