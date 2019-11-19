using Newtonsoft.Json;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SharpDX.Direct3D;
using SharpDX.Mathematics.Interop;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.InputUi;
using T3.Gui.OutputUi;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Gui
{
    public class UiModel : Core.Model
    {
        public UiModel()
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
            RegisterUiType(typeof(System.Collections.Generic.List<string>), new FloatUiProperties(), () => new StringListInputUi(),
                           () => new StringListOutputUi());
            RegisterUiType(typeof(System.Numerics.Vector2), new Size2UiProperties(), () => new Vector2InputUi(),
                           () => new ValueOutputUi<System.Numerics.Vector2>());
            RegisterUiType(typeof(System.Numerics.Vector3), new Size2UiProperties(), () => new Vector3InputUi(),
                           () => new ValueOutputUi<System.Numerics.Vector3>());
            RegisterUiType(typeof(System.Numerics.Vector4), new Size2UiProperties(), () => new Vector4InputUi(),
                           () => new ValueOutputUi<System.Numerics.Vector4>());

            // t3 core types
            RegisterUiType(typeof(Command), new FallBackUiProperties(), () => new FallbackInputUi<Command>(), () => new ValueOutputUi<Command>());

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

            Load();

            var symbols = SymbolRegistry.Entries;
            foreach (var symbolEntry in symbols)
            {
                UpdateUiEntriesForSymbol(symbolEntry.Value);
            }

            var dashboardSymbol = symbols.First(entry => entry.Value.Name == "Dashboard").Value;
            // create instance of project op, all children are create automatically
            var dashboard = dashboardSymbol.CreateInstance(Guid.NewGuid());

            Instance projectOp = dashboard.Children[0];
            MainOp = projectOp;
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
            foreach (var symbolUiEntry in SymbolUiRegistry.Entries)
            {
                using (var sw = new StreamWriter(Path + symbolUiEntry.Value.Symbol.Name + "_" + symbolUiEntry.Value.Symbol.Id + SymbolUiExtension))
                using (var writer = new JsonTextWriter(sw))
                {
                    json.Writer = writer;
                    json.Writer.Formatting = Formatting.Indented;
                    json.WriteSymbolUi(symbolUiEntry.Value);
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

        public Instance MainOp;
    }
}