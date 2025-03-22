namespace Lib.field.render;

[Guid("9323e32f-078c-4156-941b-203f4c265ff5")]
public class RaymarchFieldWithMaterial : Instance<RaymarchFieldWithMaterial>
{
    [Output(Guid = "e178ef02-c9ac-48cd-a8cb-df3aec5941bb")]
    public readonly Slot<Command> DrawCommand = new();

        [Input(Guid = "340ca675-9356-4548-ba64-732181bebeef")]
        public readonly InputSlot<T3.Core.DataTypes.ShaderGraphNode> SdfField = new InputSlot<T3.Core.DataTypes.ShaderGraphNode>();

        [Input(Guid = "3148d927-8779-47ab-9e0a-fa63206f3002")]
        public readonly InputSlot<float> MaxSteps = new InputSlot<float>();

        [Input(Guid = "561768f6-adf6-4d3d-a36a-20b6f35ff151")]
        public readonly InputSlot<float> StepSize = new InputSlot<float>();

        [Input(Guid = "0b4d60de-261f-4dbf-ad44-6395cda3a496")]
        public readonly InputSlot<float> MinDistance = new InputSlot<float>();

        [Input(Guid = "19281ea1-ae03-408a-ab63-fedd6dfc2936")]
        public readonly InputSlot<float> MaxDistance = new InputSlot<float>();

        [Input(Guid = "9715075b-b02b-4290-9332-9bbfe67933f2")]
        public readonly InputSlot<System.Numerics.Vector4> Specular = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "8001e7e1-85fa-4d91-a3f8-4fc38a01152f")]
        public readonly InputSlot<System.Numerics.Vector4> Glow_ = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "e3a85c27-b94c-4e77-b0c2-4644cd3a22d4")]
        public readonly InputSlot<System.Numerics.Vector4> AmbientOcclusion = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "cb3632b0-ae52-4848-83cb-d73ae8645fc0")]
        public readonly InputSlot<System.Numerics.Vector4> Background = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "a791e569-37f2-4d89-96a3-6cc9eaad7035")]
        public readonly InputSlot<System.Numerics.Vector2> Spec = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "ffb73f4d-6d24-4f4c-866b-5bdd6f876e6f")]
        public readonly InputSlot<float> AoDistance = new InputSlot<float>();

        [Input(Guid = "d139b8ef-0821-418e-aa45-91ecf0ff0e6f")]
        public readonly InputSlot<float> Fog = new InputSlot<float>();

        [Input(Guid = "92bb7f2e-3801-4374-9987-ddab567fd811")]
        public readonly InputSlot<System.Numerics.Vector3> LightPos = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "1251368b-f8f4-4210-be1e-4d05223caf21")]
        public readonly InputSlot<float> DistToColor = new InputSlot<float>();
}