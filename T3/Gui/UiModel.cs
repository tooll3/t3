using Newtonsoft.Json;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using SharpDX.Direct3D;
using SharpDX.Mathematics.Interop;
using T3.Compilation;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.ChildUi;
using T3.Gui.InputUi;
using T3.Gui.InputUi.SingleControl;
using T3.Gui.OutputUi;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Gui
{
    public class UiModel : Core.Model
    {
        public UiModel(Assembly operatorAssembly) 
            : base(operatorAssembly, enabledLogging: true)
        {
            Init();
        }

        private void RegisterUiType(Type type, ITypeUiProperties uiProperties,  Func<IInputUi> inputUi, Func<IOutputUi> outputUi)
        {
            TypeUiRegistry.Entries.Add(type, uiProperties);
            InputUiFactory.Entries.Add(type, inputUi);
            OutputUiFactory.Entries.Add(type, outputUi);
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
                           () => new ValueOutputUi<System.Numerics.Vector2>());
            RegisterUiType(typeof(System.Numerics.Vector3), new Size2UiProperties(), () => new Float3InputUi(),
                           () => new ValueOutputUi<System.Numerics.Vector3>());
            RegisterUiType(typeof(System.Numerics.Vector4), new Size2UiProperties(), () => new Vector4InputUi(),
                           () => new ValueOutputUi<System.Numerics.Vector4>());
            RegisterUiType(typeof(System.Text.StringBuilder), new StringUiProperties(), () => new FallbackInputUi<StringBuilder>(),
                           () => new ValueOutputUi<System.Text.StringBuilder>());

            // t3 core types
            RegisterUiType(typeof(Command), new CommandUiProperties(), () => new FallbackInputUi<Command>(), () => new CommandOutputUi());
            RegisterUiType(typeof(Core.Animation.Curve), new FloatUiProperties(), () => new CurveInputUi(),
                           () => new ValueOutputUi<Core.Animation.Curve>());
            RegisterUiType(typeof(Core.DataTypes.Gradient), new FloatUiProperties(), () => new GradientInputUi(),
                           () => new ValueOutputUi<Core.DataTypes.Gradient>());
            RegisterUiType(typeof(Core.DataTypes.ParticleSystem), new FallBackUiProperties(), () => new FallbackInputUi<Core.DataTypes.ParticleSystem>(),
                           () => new ValueOutputUi<Core.DataTypes.ParticleSystem>());

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
            RegisterUiType(typeof(SharpDX.Direct3D11.InputLayout), new ShaderUiProperties(), () => new FallbackInputUi<InputLayout>(),
                           () => new ValueOutputUi<InputLayout>());
            RegisterUiType(typeof(SharpDX.Direct3D11.PixelShader), new ShaderUiProperties(), () => new FallbackInputUi<PixelShader>(),
                           () => new ValueOutputUi<PixelShader>());
            RegisterUiType(typeof(SharpDX.Direct3D11.RasterizerState), new ShaderUiProperties(), () => new FallbackInputUi<RasterizerState>(),
                           () => new ValueOutputUi<RasterizerState>());
            RegisterUiType(typeof(SharpDX.Direct3D11.RenderTargetBlendDescription), new TextureUiProperties(), () => new FallbackInputUi<RenderTargetBlendDescription>(),
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

            // register custom UIs for symbol children
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_8211249d_7a26_4ad0_8d84_56da72a5c536.GradientSlider), GradientSliderUi.DrawChildUi);
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_5d7d61ae_0a41_4ffa_a51d_93bab665e7fe.Value), ValueUi.DrawChildUi);
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_d6384148_c654_48ce_9cf4_9adccf91283a.ValueSlider), ValueSliderUi.DrawChildUi);
            CustomChildUiRegistry.Entries.Add(typeof(Operators.Types.Id_23794a1f_372d_484b_ac31_9470d0e77819.Jitter2d), Jitter2dUi.DrawChildUi);
            
            Load();

            var symbols = SymbolRegistry.Entries;
            foreach (var symbolEntry in symbols)
            {
                UpdateUiEntriesForSymbol(symbolEntry.Value);
            }

            // create instance of project op, all children are create automatically
            Guid dashboardId = Guid.Parse("dab61a12-9996-401e-9aa6-328dd6292beb");
            var dashboardSymbol = symbols[dashboardId];
            var dashboard = dashboardSymbol.CreateInstance(Guid.NewGuid());

            Instance projectOp = dashboard;
            RootInstance = projectOp;
        }

        public override void Load()
        {
            // first load core data
            base.Load();

            UiJson json = new UiJson();
            var symbolUiFiles = Directory.GetFiles(Path, $"*{SymbolUiExtension}");
            foreach (var symbolUiFile in symbolUiFiles)
            {
                SymbolUi symbolUi = json.ReadSymbolUi(symbolUiFile);
                if (symbolUi != null)
                {
                    if (SymbolUiRegistry.Entries.ContainsKey(symbolUi.Symbol.Id))
                    {
                        Debug.Assert(false);
                    }
                    SymbolUiRegistry.Entries.Add(symbolUi.Symbol.Id, symbolUi);
                }
                else
                {
                    Log.Error("Failed reading " + symbolUiFile);
                }
            }
        }

        private string SymbolUiExtension = ".t3ui";

        public override void Save()
        {
            Log.Debug("Saving...");
            
            // first save core data
            base.Save();

            // remove all old ui files before storing to get rid off invalid ones
            DirectoryInfo di = new DirectoryInfo(Path);
            FileInfo[] files = di.GetFiles("*" + SymbolUiExtension).ToArray();
            foreach (FileInfo file in files)
            {
                File.Delete(file.FullName);
            }

            // store all symbols in corresponding files
            UiJson json = new UiJson();
            var resourceManager = ResourceManager.Instance();
            foreach (var (_, symbolUi) in SymbolUiRegistry.Entries)
            {
                var symbol = symbolUi.Symbol;
                using (var sw = new StreamWriter(Path + symbol.Name + "_" + symbol.Id + SymbolUiExtension))
                using (var writer = new JsonTextWriter(sw))
                {
                    json.Writer = writer;
                    json.Writer.Formatting = Formatting.Indented;
                    json.WriteSymbolUi(symbolUi);
                }
                
                var opResource = resourceManager.GetOperatorFileResource(Path + symbol.Name + ".cs");
                if (opResource == null)
                {
                    // if the source wasn't registered before do this now
                    resourceManager.CreateOperatorEntry(symbol.SourcePath, symbol.Id.ToString(), OperatorUpdating.Update);
                }
            }
        }

        public static SymbolUi UpdateUiEntriesForSymbol(Symbol symbol)
        {
            if (SymbolUiRegistry.Entries.TryGetValue(symbol.Id, out var symbolUi))
            {
                symbolUi.UpdateConsistencyWithSymbol();
                return symbolUi;
            }
            else
            {
                var newSymbolUi = new SymbolUi(symbol);
                SymbolUiRegistry.Entries.Add(symbol.Id, newSymbolUi);
                return newSymbolUi;
            }
        }

        public Instance RootInstance;
    }
}