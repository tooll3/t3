using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_46e60648_86f7_4eb2_a766_4f3a8237c431
{
    public class RenderImageQuad : Instance<RenderImageQuad>
    {
        [Output(Guid = "187bd947-a254-4f2e-91fc-4d96896463e9")]
        public readonly Slot<T3.Core.Command> Output = new Slot<T3.Core.Command>();


        [Input(Guid = "4d3ce02d-5efc-4d3c-9b54-8b5bb384a9ff")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "cd1368d8-c037-4b27-8bcc-af363752c5d3")]
        public readonly InputSlot<float> Width = new InputSlot<float>();

        [Input(Guid = "4471ea8d-35ca-4653-8508-6afa11dccf05")]
        public readonly InputSlot<float> Height = new InputSlot<float>();

        [Input(Guid = "6106e527-dd6c-49cf-a65f-29a88d901255")]
        public readonly InputSlot<string> ShaderPath = new InputSlot<string>();

        [Input(Guid = "be59cfb7-2023-4c10-941a-c85466c6b620")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();
    }
}

