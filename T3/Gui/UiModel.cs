using Newtonsoft.Json;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using T3.Core;
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

        private void Init()
        {
            // Register ui properties for types
            TypeUiRegistry.Entries.Add(typeof(float), new FloatUiProperties());
            TypeUiRegistry.Entries.Add(typeof(int), new IntUiProperties());
            TypeUiRegistry.Entries.Add(typeof(string), new StringUiProperties());
            TypeUiRegistry.Entries.Add(typeof(Size2), new Size2UiProperties());
            TypeUiRegistry.Entries.Add(typeof(Int3), new Size2UiProperties());
            TypeUiRegistry.Entries.Add(typeof(System.Numerics.Vector4), new Size2UiProperties());
            TypeUiRegistry.Entries.Add(typeof(ResourceUsage), new ShaderUiProperties());
            TypeUiRegistry.Entries.Add(typeof(Format), new ShaderUiProperties());
            TypeUiRegistry.Entries.Add(typeof(BindFlags), new ShaderUiProperties());
            TypeUiRegistry.Entries.Add(typeof(CpuAccessFlags), new ShaderUiProperties());
            TypeUiRegistry.Entries.Add(typeof(ResourceOptionFlags), new ShaderUiProperties());
            TypeUiRegistry.Entries.Add(typeof(ShaderResourceView), new TextureUiProperties());
            TypeUiRegistry.Entries.Add(typeof(UnorderedAccessView), new TextureUiProperties());
            TypeUiRegistry.Entries.Add(typeof(List<float>), new FloatUiProperties());
            TypeUiRegistry.Entries.Add(typeof(ComputeShader), new ShaderUiProperties());
            TypeUiRegistry.Entries.Add(typeof(Texture2D), new ShaderUiProperties());
            TypeUiRegistry.Entries.Add(typeof(Buffer), new ShaderUiProperties());
            TypeUiRegistry.Entries.Add(typeof(Filter), new ShaderUiProperties());
            TypeUiRegistry.Entries.Add(typeof(TextureAddressMode), new ShaderUiProperties());
            TypeUiRegistry.Entries.Add(typeof(Comparison), new ShaderUiProperties());
            TypeUiRegistry.Entries.Add(typeof(SamplerState), new ShaderUiProperties());
            TypeUiRegistry.Entries.Add(typeof(Scene), new FallBackUiProperties());

            // Register input ui creators
            InputUiFactory.Entries.Add(typeof(float), () => new FloatInputUi());
            InputUiFactory.Entries.Add(typeof(int), () => new IntInputUi());
            InputUiFactory.Entries.Add(typeof(string), () => new StringInputUi());
            InputUiFactory.Entries.Add(typeof(Size2), () => new Size2InputUi());
            InputUiFactory.Entries.Add(typeof(Int3), () => new Int3InputUi());
            InputUiFactory.Entries.Add(typeof(System.Numerics.Vector4), () => new Vector4InputUi());
            InputUiFactory.Entries.Add(typeof(ResourceUsage), () => new EnumInputUi<ResourceUsage>());
            InputUiFactory.Entries.Add(typeof(Format), () => new EnumInputUi<Format>());
            InputUiFactory.Entries.Add(typeof(BindFlags), () => new EnumInputUi<BindFlags>());
            InputUiFactory.Entries.Add(typeof(CpuAccessFlags), () => new EnumInputUi<CpuAccessFlags>());
            InputUiFactory.Entries.Add(typeof(ResourceOptionFlags), () => new EnumInputUi<ResourceOptionFlags>());
            InputUiFactory.Entries.Add(typeof(List<float>), () => new FloatListInputUi());
            InputUiFactory.Entries.Add(typeof(ComputeShader), () => new FallbackInputUi<ComputeShader>());
            InputUiFactory.Entries.Add(typeof(Texture2D), () => new FallbackInputUi<Texture2D>());
            InputUiFactory.Entries.Add(typeof(Buffer), () => new FallbackInputUi<Buffer>());
            InputUiFactory.Entries.Add(typeof(Filter), () => new EnumInputUi<Filter>());
            InputUiFactory.Entries.Add(typeof(TextureAddressMode), () => new EnumInputUi<TextureAddressMode>());
            InputUiFactory.Entries.Add(typeof(Comparison), () => new EnumInputUi<Comparison>());
            InputUiFactory.Entries.Add(typeof(SamplerState), () => new FallbackInputUi<SamplerState>());
            InputUiFactory.Entries.Add(typeof(ShaderResourceView), () => new FallbackInputUi<ShaderResourceView>());
            InputUiFactory.Entries.Add(typeof(UnorderedAccessView), () => new FallbackInputUi<UnorderedAccessView>());
            InputUiFactory.Entries.Add(typeof(Scene), () => new FallbackInputUi<Scene>());

            // Register output ui creators
            OutputUiFactory.Entries.Add(typeof(float), () => new ValueOutputUi<float>());
            OutputUiFactory.Entries.Add(typeof(int), () => new ValueOutputUi<int>());
            OutputUiFactory.Entries.Add(typeof(string), () => new ValueOutputUi<string>());
            OutputUiFactory.Entries.Add(typeof(Size2), () => new ValueOutputUi<Size2>());
            OutputUiFactory.Entries.Add(typeof(Int3), () => new ValueOutputUi<Int3>());
            OutputUiFactory.Entries.Add(typeof(System.Numerics.Vector4), () => new ValueOutputUi<System.Numerics.Vector4>());
            OutputUiFactory.Entries.Add(typeof(ShaderResourceView), () => new ShaderResourceViewOutputUi());
            OutputUiFactory.Entries.Add(typeof(UnorderedAccessView), () => new ValueOutputUi<UnorderedAccessView>());
            OutputUiFactory.Entries.Add(typeof(Texture2D), () => new Texture2dOutputUi());
            OutputUiFactory.Entries.Add(typeof(List<float>), () => new FloatListOutputUi());
            OutputUiFactory.Entries.Add(typeof(ComputeShader), () => new ValueOutputUi<ComputeShader>());
            OutputUiFactory.Entries.Add(typeof(Buffer), () => new ValueOutputUi<Buffer>());
            OutputUiFactory.Entries.Add(typeof(SamplerState), () => new ValueOutputUi<SamplerState>());
            OutputUiFactory.Entries.Add(typeof(Scene), () => new ValueOutputUi<Scene>());

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
                    SymbolUiRegistry.Entries.Add(symbolUi.Symbol.Id, symbolUi);
                }
            }
        }

        private string SymbolUiExtension = ".t3ui";

        public override void Save()
        {
            // first save core data
            base.Save();

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