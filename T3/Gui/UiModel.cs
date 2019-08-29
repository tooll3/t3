using Newtonsoft.Json;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using Vector2 = System.Numerics.Vector2;

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
            TypeUiRegistry.Entries.Add(typeof(ResourceUsage), new ShaderUiProperties());
            TypeUiRegistry.Entries.Add(typeof(Format), new ShaderUiProperties());
            TypeUiRegistry.Entries.Add(typeof(BindFlags), new ShaderUiProperties());
            TypeUiRegistry.Entries.Add(typeof(CpuAccessFlags), new ShaderUiProperties());
            TypeUiRegistry.Entries.Add(typeof(ResourceOptionFlags), new ShaderUiProperties());
            TypeUiRegistry.Entries.Add(typeof(ShaderResourceView), new TextureUiProperties());
            TypeUiRegistry.Entries.Add(typeof(List<float>), new FloatUiProperties());

            // Register input ui creators
            InputUiFactory.Entries.Add(typeof(float), () => new FloatInputUi());
            InputUiFactory.Entries.Add(typeof(int), () => new IntInputUi());
            InputUiFactory.Entries.Add(typeof(string), () => new StringInputUi());
            InputUiFactory.Entries.Add(typeof(Size2), () => new Size2InputUi());
            InputUiFactory.Entries.Add(typeof(ResourceUsage), () => new EnumInputUi<ResourceUsage>());
            InputUiFactory.Entries.Add(typeof(Format), () => new EnumInputUi<Format>());
            InputUiFactory.Entries.Add(typeof(BindFlags), () => new EnumInputUi<BindFlags>());
            InputUiFactory.Entries.Add(typeof(CpuAccessFlags), () => new EnumInputUi<CpuAccessFlags>());
            InputUiFactory.Entries.Add(typeof(ResourceOptionFlags), () => new EnumInputUi<ResourceOptionFlags>());
            InputUiFactory.Entries.Add(typeof(List<float>), () => new FloatListInputUi());

            // Register output ui creators
            OutputUiFactory.Entries.Add(typeof(float), () => new FloatOutputUi());
            OutputUiFactory.Entries.Add(typeof(int), () => new IntOutputUi());
            OutputUiFactory.Entries.Add(typeof(string), () => new StringOutputUi());
            OutputUiFactory.Entries.Add(typeof(Size2), () => new Size2OutputUi());
            OutputUiFactory.Entries.Add(typeof(ShaderResourceView), () => new ShaderResourceViewOutputUi());
            OutputUiFactory.Entries.Add(typeof(Texture2D), () => new Texture2dOutputUi());
            OutputUiFactory.Entries.Add(typeof(List<float>), () => new FloatListOutputUi());

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

        public void UpdateUiEntriesForSymbol(Symbol symbol)
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

        public Instance MainOp;
    }
}