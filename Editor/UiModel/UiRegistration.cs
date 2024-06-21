using System.Text;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using T3.Core.DataTypes;
using T3.Core.DataTypes.DataSet;
using T3.Core.DataTypes.Vector;
using T3.Core.Rendering.Material;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.InputUi.CombinedInputs;
using T3.Editor.Gui.InputUi.SimpleInputUis;
using T3.Editor.Gui.InputUi.SingleControl;
using T3.Editor.Gui.InputUi.VectorInputs;
using T3.Editor.Gui.OutputUi;
using Buffer = SharpDX.Direct3D11.Buffer;
using Int3 = T3.Core.DataTypes.Vector.Int3;
using Point = T3.Core.DataTypes.Point;
using Texture2D = T3.Core.DataTypes.Texture2D;
using Texture3D = T3.Core.DataTypes.Texture3D;
using PixelShader = T3.Core.DataTypes.PixelShader;
using VertexShader = T3.Core.DataTypes.VertexShader;
using ComputeShader = T3.Core.DataTypes.ComputeShader;
using GeometryShader = T3.Core.DataTypes.GeometryShader;

namespace T3.Editor.UiModel;

internal static class UiRegistration
{
    public static void RegisterUiTypes()
    {
        // build-in types
        RegisterUiType(typeof(float), new ValueUiProperties(), () => new FloatInputUi(), () => new FloatOutputUi());
        RegisterUiType(typeof(int), new ValueUiProperties(), () => new IntInputUi(), () => new ValueOutputUi<int>());
        RegisterUiType(typeof(bool), new ValueUiProperties(), () => new BoolInputUi(), () => new ValueOutputUi<bool>());
        RegisterUiType(typeof(double), new ValueUiProperties(), () => new FloatInputUi(), () => new FloatOutputUi());
        RegisterUiType(typeof(string), new StringUiProperties(), () => new StringInputUi(), () => new ValueOutputUi<string>());

        // system types
        RegisterUiType(typeof(System.Numerics.Vector2), new ValueUiProperties(), () => new Vector2InputUi(),
                       () => new VectorOutputUi<System.Numerics.Vector2>());
        RegisterUiType(typeof(System.Numerics.Vector3), new ValueUiProperties(), () => new Vector3InputUi(),
                       () => new VectorOutputUi<System.Numerics.Vector3>());
        RegisterUiType(typeof(System.Numerics.Vector4), new ValueUiProperties(), () => new Vector4InputUi(),
                       () => new VectorOutputUi<System.Numerics.Vector4>());
        RegisterUiType(typeof(System.Numerics.Quaternion), new ValueUiProperties(), () => new Vector4InputUi(),
                       () => new VectorOutputUi<System.Numerics.Quaternion>());

        RegisterUiType(typeof(System.Collections.Generic.List<float>), new ValueUiProperties(), () => new FloatListInputUi(),
                       () => new FloatListOutputUi());
        RegisterUiType(typeof(System.Collections.Generic.List<string>), new StringUiProperties(), () => new StringListInputUi(),
                       () => new StringListOutputUi());

        RegisterUiType(typeof(System.Text.StringBuilder), new StringUiProperties(), () => new FallbackInputUi<StringBuilder>(),
                       () => new ValueOutputUi<System.Text.StringBuilder>());

        RegisterUiType(typeof(DateTime), new ValueUiProperties(), () => new FallbackInputUi<DateTime>(),
                       () => new ValueOutputUi<DateTime>());

        // t3 core types
        RegisterUiType(typeof(T3.Core.DataTypes.BufferWithViews), new FallBackUiProperties(), () => new FallbackInputUi<T3.Core.DataTypes.BufferWithViews>(),
                       () => new BufferWithViewsOutputUi());
        RegisterUiType(typeof(Command), new CommandUiProperties(), () => new FallbackInputUi<Command>(), () => new CommandOutputUi());
        RegisterUiType(typeof(Curve), new ValueUiProperties(), () => new CurveInputUi(),
                       () => new ValueOutputUi<Curve>());
        RegisterUiType(typeof(T3.Core.Operator.GizmoVisibility), new FallBackUiProperties(), () => new EnumInputUi<T3.Core.Operator.GizmoVisibility>(),
                       () => new ValueOutputUi<T3.Core.Operator.GizmoVisibility>());

        RegisterUiType(typeof(T3.Core.DataTypes.Gradient), new ValueUiProperties(), () => new GradientInputUi(),
                       () => new ValueOutputUi<T3.Core.DataTypes.Gradient>());

        RegisterUiType(typeof(T3.Core.DataTypes.LegacyParticleSystem), new FallBackUiProperties(),
                       () => new FallbackInputUi<T3.Core.DataTypes.LegacyParticleSystem>(),
                       () => new ValueOutputUi<T3.Core.DataTypes.LegacyParticleSystem>());
        RegisterUiType(typeof(T3.Core.DataTypes.ParticleSystem), new FallBackUiProperties(),
                       () => new FallbackInputUi<T3.Core.DataTypes.ParticleSystem>(),
                       () => new ValueOutputUi<T3.Core.DataTypes.ParticleSystem>());
        RegisterUiType(typeof(Point[]), new PointListUiProperties(), () => new FallbackInputUi<Point[]>(),
                       () => new PointArrayOutputUi());
        RegisterUiType(typeof(T3.Core.DataTypes.RenderTargetReference), new TextureUiProperties(),
                       () => new FallbackInputUi<T3.Core.DataTypes.RenderTargetReference>(),
                       () => new ValueOutputUi<T3.Core.DataTypes.RenderTargetReference>());
        RegisterUiType(typeof(Object), new ValueUiProperties(),
                       () => new FallbackInputUi<Object>(),
                       () => new ValueOutputUi<Object>());

        RegisterUiType(typeof(T3.Core.DataTypes.StructuredList), new ValueUiProperties(), () => new StructuredListInputUi(),
                       () => new StructuredListOutputUi());

        RegisterUiType(typeof(T3.Core.DataTypes.Texture3dWithViews), new FallBackUiProperties(),
                       () => new FallbackInputUi<T3.Core.DataTypes.Texture3dWithViews>(),
                       () => new Texture3dOutputUi());

        // Rendering
        RegisterUiType(typeof(MeshBuffers), new FallBackUiProperties(), () => new FallbackInputUi<MeshBuffers>(),
                       () => new ValueOutputUi<MeshBuffers>());

        RegisterUiType(typeof(DataSet), new FallBackUiProperties(),
                       () => new FallbackInputUi<DataSet>(), () => new DataSetOutputUi());

        RegisterUiType(typeof(SceneSetup), new FallBackUiProperties(),
                       () => new SceneSetupInputUi(), () => new SceneSetupOutputUi());

        RegisterUiType(typeof(PbrMaterial), new FallBackUiProperties(),
                       () => new FallbackInputUi<PbrMaterial>(),
                       () => new ValueOutputUi<PbrMaterial>());

        // sharpdx types
        RegisterUiType(typeof(Int3), new ValueUiProperties(), () => new Int3InputUi(), () => new ValueOutputUi<Int3>());
        RegisterUiType(typeof(Int2), new ValueUiProperties(), () => new Int2InputUi(), () => new ValueOutputUi<Int2>());
        RegisterUiType(typeof(SharpDX.Direct3D.PrimitiveTopology), new FallBackUiProperties(), () => new EnumInputUi<PrimitiveTopology>(),
                       () => new ValueOutputUi<PrimitiveTopology>());
        RegisterUiType(typeof(SharpDX.Direct3D11.BindFlags), new ShaderUiProperties(), () => new EnumInputUi<BindFlags>(),
                       () => new ValueOutputUi<BindFlags>());
        RegisterUiType(typeof(SharpDX.Direct3D11.BlendOperation), new ShaderUiProperties(), () => new EnumInputUi<BlendOperation>(),
                       () => new ValueOutputUi<BlendOperation>());
        RegisterUiType(typeof(SharpDX.Direct3D11.BlendOption), new ShaderUiProperties(), () => new EnumInputUi<BlendOption>(),
                       () => new ValueOutputUi<BlendOption>());
        RegisterUiType(typeof(SharpDX.Direct3D11.BlendState), new ShaderUiProperties(), () => new FallbackInputUi<BlendState>(),
                       () => new ValueOutputUi<BlendState>());
        RegisterUiType(typeof(SharpDX.Direct3D11.Buffer), new ShaderUiProperties(), () => new FallbackInputUi<Buffer>(), () => new ValueOutputUi<Buffer>());
        RegisterUiType(typeof(SharpDX.Direct3D11.ColorWriteMaskFlags), new ShaderUiProperties(), () => new EnumInputUi<ColorWriteMaskFlags>(),
                       () => new ValueOutputUi<ColorWriteMaskFlags>());
        RegisterUiType(typeof(SharpDX.Direct3D11.Comparison), new ShaderUiProperties(), () => new EnumInputUi<Comparison>(),
                       () => new ValueOutputUi<Comparison>());
        RegisterUiType(typeof(ComputeShader), new ShaderUiProperties(), () => new FallbackInputUi<ComputeShader>(),
                       () => new ValueOutputUi<ComputeShader>());
        RegisterUiType(typeof(SharpDX.Direct3D11.CpuAccessFlags), new ShaderUiProperties(), () => new EnumInputUi<CpuAccessFlags>(),
                       () => new ValueOutputUi<CpuAccessFlags>());
        RegisterUiType(typeof(SharpDX.Direct3D11.CullMode), new ShaderUiProperties(), () => new EnumInputUi<CullMode>(),
                       () => new ValueOutputUi<CullMode>());
        RegisterUiType(typeof(SharpDX.Direct3D11.DepthStencilState), new ShaderUiProperties(), () => new FallbackInputUi<DepthStencilState>(),
                       () => new ValueOutputUi<DepthStencilState>());
        RegisterUiType(typeof(SharpDX.Direct3D11.DepthStencilView), new TextureUiProperties(), () => new FallbackInputUi<DepthStencilView>(),
                       () => new ValueOutputUi<DepthStencilView>());
        RegisterUiType(typeof(SharpDX.Direct3D11.FillMode), new ShaderUiProperties(), () => new EnumInputUi<FillMode>(),
                       () => new ValueOutputUi<FillMode>());
        RegisterUiType(typeof(SharpDX.Direct3D11.Filter), new ShaderUiProperties(), () => new EnumInputUi<Filter>(), () => new ValueOutputUi<Filter>());
        RegisterUiType(typeof(GeometryShader), new ShaderUiProperties(), () => new FallbackInputUi<GeometryShader>(),
                       () => new ValueOutputUi<GeometryShader>());

        RegisterUiType(typeof(SharpDX.Direct3D11.InputLayout), new ShaderUiProperties(), () => new FallbackInputUi<InputLayout>(),
                       () => new ValueOutputUi<InputLayout>());
        RegisterUiType(typeof(PixelShader), new ShaderUiProperties(), () => new FallbackInputUi<PixelShader>(),
                       () => new ValueOutputUi<PixelShader>());
        RegisterUiType(typeof(SharpDX.Direct3D11.RasterizerState), new ShaderUiProperties(), () => new FallbackInputUi<RasterizerState>(),
                       () => new ValueOutputUi<RasterizerState>());
        RegisterUiType(typeof(SharpDX.Direct3D11.RenderTargetBlendDescription), new TextureUiProperties(),
                       () => new FallbackInputUi<RenderTargetBlendDescription>(),
                       () => new ValueOutputUi<RenderTargetBlendDescription>());
        RegisterUiType(typeof(SharpDX.Direct3D11.RenderTargetView), new TextureUiProperties(), () => new FallbackInputUi<RenderTargetView>(),
                       () => new ValueOutputUi<RenderTargetView>());
        RegisterUiType(typeof(SharpDX.Direct3D11.ResourceOptionFlags), new ShaderUiProperties(), () => new EnumInputUi<ResourceOptionFlags>(),
                       () => new ValueOutputUi<ResourceOptionFlags>());
        RegisterUiType(typeof(SharpDX.Direct3D11.ResourceUsage), new ShaderUiProperties(), () => new EnumInputUi<ResourceUsage>(),
                       () => new ValueOutputUi<ResourceUsage>());
        RegisterUiType(typeof(SharpDX.Direct3D11.SamplerState), new ShaderUiProperties(), () => new FallbackInputUi<SamplerState>(),
                       () => new ValueOutputUi<SamplerState>());
        RegisterUiType(typeof(SharpDX.Direct3D11.ShaderResourceView), new TextureUiProperties(), () => new FallbackInputUi<ShaderResourceView>(),
                       () => new ShaderResourceViewOutputUi());
        RegisterUiType(typeof(Texture2D), new TextureUiProperties(), () => new FallbackInputUi<Texture2D>(),
                       () => new Texture2dOutputUi());
        RegisterUiType(typeof(Texture3D), new ShaderUiProperties(), () => new FallbackInputUi<Texture3D>(),
                       () => new ValueOutputUi<SharpDX.Direct3D11.Texture3D>());
        RegisterUiType(typeof(SharpDX.Direct3D11.TextureAddressMode), new ShaderUiProperties(), () => new EnumInputUi<TextureAddressMode>(),
                       () => new ValueOutputUi<TextureAddressMode>());
        RegisterUiType(typeof(SharpDX.Direct3D11.UnorderedAccessView), new TextureUiProperties(), () => new FallbackInputUi<UnorderedAccessView>(),
                       () => new ValueOutputUi<UnorderedAccessView>());
        RegisterUiType(typeof(SharpDX.Direct3D11.UnorderedAccessViewBufferFlags), new ShaderUiProperties(),
                       () => new EnumInputUi<UnorderedAccessViewBufferFlags>(),
                       () => new ValueOutputUi<UnorderedAccessViewBufferFlags>());
        RegisterUiType(typeof(VertexShader), new ShaderUiProperties(), () => new FallbackInputUi<VertexShader>(),
                       () => new ValueOutputUi<VertexShader>());
        RegisterUiType(typeof(SharpDX.DXGI.Format), new ShaderUiProperties(), () => new EnumInputUi<Format>(), () => new ValueOutputUi<Format>());
        RegisterUiType(typeof(SharpDX.Mathematics.Interop.RawViewportF), new ShaderUiProperties(), () => new FallbackInputUi<RawViewportF>(),
                       () => new ValueOutputUi<RawViewportF>());
        RegisterUiType(typeof(SharpDX.Mathematics.Interop.RawRectangle), new ShaderUiProperties(), () => new FallbackInputUi<RawRectangle>(),
                       () => new ValueOutputUi<RawRectangle>());
        RegisterUiType(typeof(System.Numerics.Vector4[]), new PointListUiProperties(), () => new FallbackInputUi<System.Numerics.Vector4[]>(),
                       () => new ValueOutputUi<System.Numerics.Vector4[]>());
        RegisterUiType(typeof(Dict<float>), new ValueUiProperties(),
                       () => new FloatDictInputUi(), () => new FloatDictOutputUi());
        return;

        static void RegisterUiType(Type type, ITypeUiProperties uiProperties, Func<IInputUi> inputUi, Func<IOutputUi> outputUi)
        {
            TypeUiRegistry.Entries.Add(type, uiProperties);
            InputUiFactory.Entries.Add(type, inputUi);
            OutputUiFactory.Entries.Add(type, outputUi);
        }
    }
}