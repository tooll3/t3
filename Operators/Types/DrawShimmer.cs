using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Operators.Types.Id_fd9bffd3_5c57_462f_8761_85f94c5a629b;

namespace T3.Operators.Types.Id_c87b140b_1109_4eff_bf77_98bff3fc3e17
{
    public class DrawShimmer : Instance<DrawShimmer>
    {
        [Output(Guid = "e44490fc-bf15-4e36-8b81-4d7c45949dbc")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "3ff3dc96-90a3-426f-a660-762de6cb54b0")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "724d28d3-5b04-4e82-9d4f-01e7d42b9577")]
        public readonly InputSlot<float> NoiseComplexity = new InputSlot<float>();

        [Input(Guid = "338a0765-027c-4f49-a5de-38c1ab1035ef")]
        public readonly InputSlot<float> DistributionNoise = new InputSlot<float>();

        [Input(Guid = "d0b327fa-f48a-44cd-aa01-161b1c61bdc1")]
        public readonly InputSlot<float> IntensityNoise = new InputSlot<float>();

        [Input(Guid = "bab504e1-dce7-4264-b1c8-40ec14202305")]
        public readonly InputSlot<float> CoreIntensity = new InputSlot<float>();

        [Input(Guid = "a2785b57-abcc-442e-90c7-cd120d3255ab")]
        public readonly InputSlot<float> Gamma = new InputSlot<float>();

        [Input(Guid = "f1561cc1-3d5a-4caf-899e-fc0c81eed15b")]
        public readonly InputSlot<float> Colorize = new InputSlot<float>();

        [Input(Guid = "1191708c-9cdb-4331-8886-1742193f532e")]
        public readonly InputSlot<float> AnimationSpeed = new InputSlot<float>();

        [Input(Guid = "f9070783-1989-4fcd-9db1-f370f36be71e")]
        public readonly InputSlot<float> NoiseProgress = new InputSlot<float>();

        [Input(Guid = "7c6c2ad1-b7d5-4496-822e-1720356b59d7")]
        public readonly InputSlot<bool> MultiSampling4x = new InputSlot<bool>();

        [Input(Guid = "66aeff41-c6af-4a06-9813-e526308968c2")]
        public readonly InputSlot<float> CircularCompletion = new InputSlot<float>();

        [Input(Guid = "647fcd91-534c-4433-a338-e1ac77ec5f2b")]
        public readonly InputSlot<float> CircularCompletionEdge = new InputSlot<float>();
    }
}

