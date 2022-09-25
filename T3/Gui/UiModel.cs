using Newtonsoft.Json;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using SharpDX.Direct3D;
using SharpDX.Mathematics.Interop;
using T3.Compilation;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Gui.ChildUi;
using T3.Gui.InputUi;
using t3.Gui.InputUi.SimpleInputUis;
using T3.Gui.InputUi.SingleControl;
using T3.Gui.InputUi.VectorInputs;
using T3.Gui.OutputUi;
using Buffer = SharpDX.Direct3D11.Buffer;
using Point = T3.Core.DataTypes.Point;

// ReSharper disable RedundantNameQualifier

namespace T3.Gui
{
    public static class TypeByNameRegistry
    {
        public static Dictionary<string, Type> Entries { get; } = new Dictionary<string, Type>();
    }

    public class UiModel : Core.Model
    {
        public UiModel(Assembly operatorAssembly)
            : base(operatorAssembly)
        {
            Init();
        }

        private void RegisterUiType(Type type, ITypeUiProperties uiProperties, Func<IInputUi> inputUi, Func<IOutputUi> outputUi)
        {
            TypeUiRegistry.Entries.Add(type, uiProperties);
            InputUiFactory.Entries.Add(type, inputUi);
            OutputUiFactory.Entries.Add(type, outputUi);

            var typeFullName = type.ToString();
            TypeByNameRegistry.Entries[typeFullName] = type;
        }

        private void Init()
        {
            // build-in types
            RegisterUiType(typeof(bool), new IntUiProperties(), () => new BoolInputUi(), () => new ValueOutputUi<bool>());
            RegisterUiType(typeof(float), new FloatUiProperties(), () => new FloatInputUi(), () => new FloatOutputUi());
            RegisterUiType(typeof(int), new IntUiProperties(), () => new IntInputUi(), () => new ValueOutputUi<int>());
            RegisterUiType(typeof(string), new StringUiProperties(), () => new StringInputUi(), () => new ValueOutputUi<string>());

            // system types
            RegisterUiType(typeof(System.Collections.Generic.List<float>), new FloatUiProperties(), () => new FloatListInputUi(),
                           () => new FloatListOutputUi());
            RegisterUiType(typeof(System.Collections.Generic.List<string>), new StringUiProperties(), () => new StringListInputUi(),
                           () => new StringListOutputUi());
            RegisterUiType(typeof(System.Numerics.Vector2), new Size2UiProperties(), () => new Float2InputUi(),
                           () => new VectorOutputUi<System.Numerics.Vector2>());
            RegisterUiType(typeof(System.Numerics.Vector3), new Size2UiProperties(), () => new Float3InputUi(),
                           () => new VectorOutputUi<System.Numerics.Vector3>());
            RegisterUiType(typeof(System.Numerics.Vector4), new Size2UiProperties(), () => new Float4InputUi(),
                           () => new VectorOutputUi<System.Numerics.Vector4>());
            RegisterUiType(typeof(System.Text.StringBuilder), new StringUiProperties(), () => new FallbackInputUi<StringBuilder>(),
                           () => new ValueOutputUi<System.Text.StringBuilder>());

            RegisterUiType(typeof(DateTime), new FloatUiProperties(), () => new FallbackInputUi<DateTime>(),
                           () => new ValueOutputUi<DateTime>());

            // t3 core types
            RegisterUiType(typeof(Core.DataTypes.BufferWithViews), new FallBackUiProperties(), () => new FallbackInputUi<Core.DataTypes.BufferWithViews>(),
                           () => new ValueOutputUi<Core.DataTypes.BufferWithViews>());
            RegisterUiType(typeof(Command), new CommandUiProperties(), () => new FallbackInputUi<Command>(), () => new CommandOutputUi());
            RegisterUiType(typeof(Core.Animation.Curve), new FloatUiProperties(), () => new CurveInputUi(),
                           () => new ValueOutputUi<Core.Animation.Curve>());
            RegisterUiType(typeof(T3.Core.Operator.GizmoVisibility), new FallBackUiProperties(), () => new EnumInputUi<T3.Core.Operator.GizmoVisibility>(),
                           () => new ValueOutputUi<T3.Core.Operator.GizmoVisibility>());
            RegisterUiType(typeof(Core.DataTypes.Gradient), new FloatUiProperties(), () => new GradientInputUi(),
                           () => new ValueOutputUi<Core.DataTypes.Gradient>());
            RegisterUiType(typeof(Core.DataTypes.ParticleSystem), new FallBackUiProperties(), () => new FallbackInputUi<Core.DataTypes.ParticleSystem>(),
                           () => new ValueOutputUi<Core.DataTypes.ParticleSystem>());
            RegisterUiType(typeof(Core.DataTypes.Point[]), new PointListUiProperties(), () => new FallbackInputUi<Point[]>(),
                           () => new PointArrayOutputUi());
            RegisterUiType(typeof(Core.DataTypes.RenderTargetReference), new TextureUiProperties(),
                           () => new FallbackInputUi<Core.DataTypes.RenderTargetReference>(),
                           () => new ValueOutputUi<Core.DataTypes.RenderTargetReference>());
            RegisterUiType(typeof(Object), new FloatUiProperties(),
                           () => new FallbackInputUi<Object>(),
                           () => new ValueOutputUi<Object>());

            RegisterUiType(typeof(Core.DataTypes.StructuredList), new FloatUiProperties(), () => new StructuredListInputUi(),
                           () => new StructuredListOutputUi());
            RegisterUiType(typeof(Core.DataTypes.Texture3dWithViews), new FallBackUiProperties(),
                           () => new FallbackInputUi<Core.DataTypes.Texture3dWithViews>(),
                           () => new Texture3dOutputUi());

            RegisterUiType(typeof(MeshBuffers), new FallBackUiProperties(), () => new FallbackInputUi<MeshBuffers>(),
                           () => new ValueOutputUi<MeshBuffers>());

            // sharpdx types
            RegisterUiType(typeof(SharpDX.Int3), new Size2UiProperties(), () => new Int3InputUi(), () => new ValueOutputUi<Int3>());
            RegisterUiType(typeof(SharpDX.Size2), new Size2UiProperties(), () => new Size2InputUi(), () => new ValueOutputUi<Size2>());
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
            RegisterUiType(typeof(SharpDX.Direct3D11.Texture2D), new ShaderUiProperties(), () => new FallbackInputUi<Texture2D>(),
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
            RegisterUiType(typeof(SharpDX.Vector4[]), new PointListUiProperties(), () => new FallbackInputUi<SharpDX.Vector4[]>(),
                           () => new ValueOutputUi<SharpDX.Vector4[]>());
            RegisterUiType(typeof(Dict<float>), new FloatUiProperties(),
                           () => new FloatDictInputUi(), () => new FloatDictOutputUi());

            // register custom UIs for symbol children
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_11882635_4757_4cac_a024_70bb4e8b504c.Counter), CounterUi.DrawChildUi);
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_000e08d0_669f_48df_9083_7aa0a43bbc05.GpuMeasure), GpuMeasureUi.DrawChildUi);
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_8211249d_7a26_4ad0_8d84_56da72a5c536.SampleGradient), GradientSliderUi.DrawChildUi);
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_b724ea74_d5d7_4928_9cd1_7a7850e4e179.SampleCurve), SampleCurveUi.DrawChildUi);
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_3b0eb327_6ad8_424f_bca7_ccbfa2c9a986.Jitter), JitterUi.DrawChildUi);
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_23794a1f_372d_484b_ac31_9470d0e77819.Jitter2d), Jitter2dUi.DrawChildUi);
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_5880cbc3_a541_4484_a06a_0e6f77cdbe8e.String), StringUi.DrawChildUi);
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_5d7d61ae_0a41_4ffa_a51d_93bab665e7fe.Value), ValueUi.DrawChildUi);
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_cc07b314_4582_4c2c_84b8_bb32f59fc09b.IntValue), IntValueUi.DrawChildUi);
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_d6384148_c654_48ce_9cf4_9adccf91283a.ValueSlider), ValueSliderUi.DrawChildUi);
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_f0acd1a4_7a98_43ab_a807_6d1bd3e92169.Remap), RemapUi.DrawChildUi);
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_c5e39c67_256f_4cb9_a635_b62a0d9c796c.AnimValue), AnimValueUi.DrawChildUi);
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_94a392e6_3e03_4ccf_a114_e6fafa263b4f.SequenceAnim), SequenceAnimUi.DrawChildUi);
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_95d586a2_ee14_4ff5_a5bb_40c497efde95.TriggerAnim), TriggerAnimUi.DrawChildUi);
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_59a0458e_2f3a_4856_96cd_32936f783cc5.MidiInput), MidiInputUi.DrawChildUi);
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_bfe540ef_f8ad_45a2_b557_cd419d9c8e44.DataList), DataListUi.DrawChildUi);
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_ed0f5188_8888_453e_8db4_20d87d18e9f4.Boolean), BooleanUi.DrawChildUi);
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_0bec016a_5e1b_467a_8273_368d4d6b9935.Trigger), TriggerUi.DrawChildUi);

            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_be52b670_9749_4c0d_89f0_d8b101395227.LoadObj), DescriptiveUi.DrawChildUi);
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_a256d70f_adb3_481d_a926_caf35bd3e64c.ComputeShader), DescriptiveUi.DrawChildUi);
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_646f5988_0a76_4996_a538_ba48054fd0ad.VertexShader), DescriptiveUi.DrawChildUi);
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_f7c625da_fede_4993_976c_e259e0ee4985.PixelShader), DescriptiveUi.DrawChildUi);

            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_03477b9a_860e_4887_81c3_5fe51621122c.AudioReaction), AudioReactionUi.DrawChildUi);

            foreach (var symbolEntry in SymbolRegistry.Entries)
            {
                var valueInstanceType = symbolEntry.Value.InstanceType;
                if (typeof(IDescriptiveGraphNode).IsAssignableFrom(valueInstanceType))
                {
                    CustomChildUiRegistry.Entries.Add(valueInstanceType, DescriptiveUi.DrawChildUi);
                }
            }

            Load();

            var symbols = SymbolRegistry.Entries;
            foreach (var symbolEntry in symbols)
            {
                UpdateUiEntriesForSymbol(symbolEntry.Value);
            }

            // create instance of project op, all children are create automatically
            
            var dashboardSymbol = symbols[DashboardSymbolId];
            
            Guid dashboardInstanceId = Guid.Parse("12d48d5a-b8f4-4e08-8d79-4438328662f0");
            RootInstance = dashboardSymbol.CreateInstance(dashboardInstanceId);
        }
        
        public static Guid DashboardSymbolId = Guid.Parse("dab61a12-9996-401e-9aa6-328dd6292beb");

        public override void Load()
        {
            // first load core data
            base.Load();

            var uiJson = new SymbolUiJson();

            var symbolUiFiles = Directory.GetFiles(OperatorTypesFolder, $"*{SymbolUiExtension}", SearchOption.AllDirectories);

            foreach (var symbolUiFile in symbolUiFiles)
            {
                var symbolUi = uiJson.ReadSymbolUi(symbolUiFile);
                if (symbolUi != null)
                {
                    if (SymbolUiRegistry.Entries.ContainsKey(symbolUi.Symbol.Id))
                    {
                        Log.Error($"Can't load UI for [{symbolUi.Symbol.Name}] Registry already contains id {symbolUi.Symbol.Id}.");
                        continue;
                    }

                    Log.Debug($"Add UI for {symbolUi.Symbol.Name} {symbolUi.Symbol.Id}");
                    SymbolUiRegistry.Entries.Add(symbolUi.Symbol.Id, symbolUi);
                }
                else
                {
                    Log.Error("Failed reading " + symbolUiFile);
                }
            }
        }

        public override void SaveAll()
        {
            Log.Debug("Saving...");
            IsSaving = true;

            // Save all t3 and source files
            base.SaveAll();

            // Remove all old ui files before storing to get rid off invalid ones
            var symbolUiFiles = Directory.GetFiles(OperatorTypesFolder, $"*{SymbolUiExtension}", SearchOption.AllDirectories);            
            foreach (var filepath in symbolUiFiles)
            {
                try
                {
                    File.Delete(filepath);
                }
                catch (Exception e)
                {
                    Log.Warning("Failed to deleted file '" + filepath + "': " + e);
                }
            }

            WriteSymbolUis(SymbolUiRegistry.Entries.Values);

            IsSaving = false;
        }

        public static IEnumerable<SymbolUi> GetModifiedSymbolUis()
        {
            return SymbolUiRegistry.Entries.Values.Where(symbolUi => symbolUi.HasBeenModified);
        }

        /// <summary>
        /// Note: This does NOT clean up 
        /// </summary>
        public void SaveModifiedSymbols()
        {
            var modifiedSymbolUis = GetModifiedSymbolUis().ToList();
            Log.Debug($"Saving {modifiedSymbolUis.Count} modified symbols...");

            IsSaving = true;
            ResourceManager.Instance().DisableOperatorFileWatcher(); // Don't update ops if file is written during save
            
            var modifiedSymbols = modifiedSymbolUis.Select(symbolUi => symbolUi.Symbol).ToList();
            SaveSymbolDefinitionAndSourceFiles(modifiedSymbols);
            WriteSymbolUis(modifiedSymbolUis);
            
            ResourceManager.Instance().EnableOperatorFileWatcher();
            IsSaving = false;
        }

        private static void WriteSymbolUis(IEnumerable<SymbolUi> symbolUis)
        {
            
            var json = new SymbolUiJson();
            var resourceManager = ResourceManager.Instance();

            foreach (var symbolUi in symbolUis)
            {
                var symbol = symbolUi.Symbol;
                var filepath = BuildFilepathForSymbol(symbol, SymbolUiExtension);

                using (var sw = new StreamWriter(filepath))
                using (var writer = new JsonTextWriter(sw))
                {
                    json.Writer = writer;
                    json.Writer.Formatting = Formatting.Indented;
                    json.WriteSymbolUi(symbolUi);
                }

                var symbolSourceFilepath = BuildFilepathForSymbol(symbol, Model.SourceExtension);
                var opResource = resourceManager.GetOperatorFileResource(symbolSourceFilepath);
                if (opResource == null)
                {
                    // If the source wasn't registered before do this now
                    resourceManager.CreateOperatorEntry(symbolSourceFilepath, symbol.Id.ToString(), OperatorUpdating.ResourceUpdateHandler);
                }

                symbolUi.ClearModifiedFlag();
            }
        }

        public bool IsSaving { get; private set; }

        public static void UpdateUiEntriesForSymbol(Symbol symbol)
        {
            if (SymbolUiRegistry.Entries.TryGetValue(symbol.Id, out var symbolUi))
            {
                symbolUi.UpdateConsistencyWithSymbol();
            }
            else
            {
                var newSymbolUi = new SymbolUi(symbol);
                SymbolUiRegistry.Entries.Add(symbol.Id, newSymbolUi);
            }
        }

        public Instance RootInstance { get; private set; }
    }
}