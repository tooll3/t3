namespace Lib.field.render;

[Guid("83e4575c-2190-4c8d-b952-60b20b6c3489")]
public class RaymarchField : Instance<RaymarchField>
{
    [Output(Guid = "6cfa828f-e874-4c5c-ba2e-6db538490bfd")]
    public readonly Slot<Command> DrawCommand = new();

        [Input(Guid = "16a89f4b-c426-45d5-98f6-f0e84326e46d")]
        public readonly InputSlot<T3.Core.DataTypes.ShaderGraphNode> SdfField = new InputSlot<T3.Core.DataTypes.ShaderGraphNode>();

        [Input(Guid = "596e7b55-06c2-4988-b9a2-67c635ad47b7")]
        public readonly InputSlot<float> MaxSteps = new InputSlot<float>();

        [Input(Guid = "a589b7ad-03d7-48bb-942d-3c93b047af0e")]
        public readonly InputSlot<float> StepSize = new InputSlot<float>();

        [Input(Guid = "4256209c-5cf4-4b8b-a813-fd0c37495981")]
        public readonly InputSlot<float> MinDistance = new InputSlot<float>();

        [Input(Guid = "779f396e-c28a-4b67-babe-e16e12b49f4d")]
        public readonly InputSlot<float> MaxDistance = new InputSlot<float>();

        [Input(Guid = "2d25481c-4247-4ce1-b3b9-8c19f1fcd1c5")]
        public readonly InputSlot<System.Numerics.Vector4> Specular = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "7bdc7eb1-e8ec-4861-9408-e92f63a6ba65")]
        public readonly InputSlot<System.Numerics.Vector4> Glow_ = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "b2924806-ea7a-4e94-98ac-1c10ae96870e")]
        public readonly InputSlot<System.Numerics.Vector4> AmbientOcclusion = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "8c80d2fc-af31-45a4-8f00-dc02b2e51808")]
        public readonly InputSlot<System.Numerics.Vector4> Background = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "7def17fd-dcf2-481a-bfc1-e0c05a8131a6")]
        public readonly InputSlot<System.Numerics.Vector2> Spec = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "00874001-e2ff-45a4-891d-79f1a4049b4e")]
        public readonly InputSlot<float> AoDistance = new InputSlot<float>();

        [Input(Guid = "f141edef-a8da-4694-abfc-aef6898adf1c")]
        public readonly InputSlot<float> Fog = new InputSlot<float>();

        [Input(Guid = "50d24336-51e5-45c5-afbb-538e9f50694c")]
        public readonly InputSlot<System.Numerics.Vector3> LightPos = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "a96601d0-7d49-46d4-b48a-5b4dcd82b9a6")]
        public readonly InputSlot<float> DistToColor = new InputSlot<float>();
}