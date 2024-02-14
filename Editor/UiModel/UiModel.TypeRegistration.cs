using System;
using System.Collections.Generic;
using System.Text;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using T3.Core.DataTypes;
using T3.Core.DataTypes.DataSet;
using T3.Core.DataTypes.Vector;
using T3.Core.Rendering.Material;
using T3.Editor.Gui.ChildUi;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.InputUi.CombinedInputs;
using T3.Editor.Gui.InputUi.SimpleInputUis;
using T3.Editor.Gui.InputUi.SingleControl;
using T3.Editor.Gui.InputUi.VectorInputs;
using T3.Editor.Gui.OutputUi;
using Buffer = SharpDX.Direct3D11.Buffer;
using Int3 = T3.Core.DataTypes.Vector.Int3;
using Point = T3.Core.DataTypes.Point;

namespace T3.Editor.UiModel;

public partial class UiSymbolData
{
    static UiSymbolData()
    {
        RegisterUiTypes();
        RegisterCustomChildUis();
    }

    public static class TypeByNameRegistry
    {
        public static Dictionary<string, Type> Entries { get; } = new();
    }

    private static void RegisterCustomChildUis()
    {
        // register custom UIs for symbol children
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_11882635_4757_4cac_a024_70bb4e8b504c.Counter), CounterUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_000e08d0_669f_48df_9083_7aa0a43bbc05.GpuMeasure), GpuMeasureUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_8211249d_7a26_4ad0_8d84_56da72a5c536.SampleGradient), GradientSliderUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_b724ea74_d5d7_4928_9cd1_7a7850e4e179.SampleCurve), SampleCurveUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_3b0eb327_6ad8_424f_bca7_ccbfa2c9a986._Jitter), _JitterUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_23794a1f_372d_484b_ac31_9470d0e77819._Jitter2d), Jitter2dUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_5880cbc3_a541_4484_a06a_0e6f77cdbe8e.AString), AStringUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_5d7d61ae_0a41_4ffa_a51d_93bab665e7fe.Value), ValueUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_cc07b314_4582_4c2c_84b8_bb32f59fc09b.IntValue), IntValueUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_f0acd1a4_7a98_43ab_a807_6d1bd3e92169.Remap), RemapUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_ea7b8491_2f8e_4add_b0b1_fd068ccfed0d.AnimValue), AnimValueUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_af79ee8c_d08d_4dca_b478_b4542ed69ad8.AnimVec2), AnimVec2Ui.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_7814fd81_b8d0_4edf_b828_5165f5657344.AnimVec3), AnimVec3Ui.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_94a392e6_3e03_4ccf_a114_e6fafa263b4f.SequenceAnim), SequenceAnimUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_95d586a2_ee14_4ff5_a5bb_40c497efde95.TriggerAnim), TriggerAnimUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_59a0458e_2f3a_4856_96cd_32936f783cc5.MidiInput), MidiInputUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_bfe540ef_f8ad_45a2_b557_cd419d9c8e44.DataList), DataListUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_ed0f5188_8888_453e_8db4_20d87d18e9f4.Boolean), BooleanUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_0bec016a_5e1b_467a_8273_368d4d6b9935.Trigger), TriggerUi.DrawChildUi);

        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_be52b670_9749_4c0d_89f0_d8b101395227.LoadObj), DescriptiveUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_00618c91_f39a_44ea_b9d8_175c996460dc.LoadGltfScene), DescriptiveUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_a256d70f_adb3_481d_a926_caf35bd3e64c.ComputeShader), DescriptiveUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_646f5988_0a76_4996_a538_ba48054fd0ad.VertexShader), DescriptiveUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_f7c625da_fede_4993_976c_e259e0ee4985.PixelShader), DescriptiveUi.DrawChildUi);

        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_470db771_c7f2_4c52_8897_d3a9b9fc6a4e.GetIntVar), GetIntVarUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_e6072ecf_30d2_4c52_afa1_3b195d61617b.GetFloatVar), GetFloatVarUi.DrawChildUi);
        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_2a0c932a_eb81_4a7d_aeac_836a23b0b789.SetFloatVar), SetFloatVarUi.DrawChildUi);

        CustomChildUiRegistry.Entries.Add(typeof(T3.Operators.Types.Id_03477b9a_860e_4887_81c3_5fe51621122c.AudioReaction), AudioReactionUi.DrawChildUi);
    }

    private static void RegisterUiTypes()
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
        
        
        RegisterUiType(typeof(T3.Core.DataTypes.LegacyParticleSystem), new FallBackUiProperties(), () => new FallbackInputUi<T3.Core.DataTypes.LegacyParticleSystem>(),
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
        RegisterUiType(typeof(SharpDX.Direct3D11.PixelShader), new ShaderUiProperties(), () => new FallbackInputUi<PixelShader>(),
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
        RegisterUiType(typeof(SharpDX.Direct3D11.Texture2D), new TextureUiProperties(), () => new FallbackInputUi<Texture2D>(),
                       () => new Texture2dOutputUi());
        RegisterUiType(typeof(SharpDX.Direct3D11.Texture3D), new ShaderUiProperties(), () => new FallbackInputUi<Texture3D>(),
                       () => new ValueOutputUi<SharpDX.Direct3D11.Texture3D>());
        RegisterUiType(typeof(SharpDX.Direct3D11.TextureAddressMode), new ShaderUiProperties(), () => new EnumInputUi<TextureAddressMode>(),
                       () => new ValueOutputUi<TextureAddressMode>());
        RegisterUiType(typeof(SharpDX.Direct3D11.UnorderedAccessView), new TextureUiProperties(), () => new FallbackInputUi<UnorderedAccessView>(),
                       () => new ValueOutputUi<UnorderedAccessView>());
        RegisterUiType(typeof(SharpDX.Direct3D11.UnorderedAccessViewBufferFlags), new ShaderUiProperties(),
                       () => new EnumInputUi<UnorderedAccessViewBufferFlags>(),
                       () => new ValueOutputUi<UnorderedAccessViewBufferFlags>());
        RegisterUiType(typeof(SharpDX.Direct3D11.VertexShader), new ShaderUiProperties(), () => new FallbackInputUi<VertexShader>(),
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
    }

    private static void RegisterUiType(Type type, ITypeUiProperties uiProperties, Func<IInputUi> inputUi, Func<IOutputUi> outputUi)
    {
        TypeUiRegistry.Entries.Add(type, uiProperties);
        InputUiFactory.Entries.Add(type, inputUi);
        OutputUiFactory.Entries.Add(type, outputUi);

        var typeFullName = type.ToString();
        TypeByNameRegistry.Entries[typeFullName] = type;
    }
}