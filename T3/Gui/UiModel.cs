using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
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
            TypeUiRegistry.Entries.Add(typeof(ResourceUsage), new Size2UiProperties());
            TypeUiRegistry.Entries.Add(typeof(Format), new Size2UiProperties());
            TypeUiRegistry.Entries.Add(typeof(BindFlags), new Size2UiProperties());
            TypeUiRegistry.Entries.Add(typeof(CpuAccessFlags), new Size2UiProperties());
            TypeUiRegistry.Entries.Add(typeof(ResourceOptionFlags), new Size2UiProperties());
            TypeUiRegistry.Entries.Add(typeof(ShaderResourceView), new Size2UiProperties());

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

            // Register output ui creators
            OutputUiFactory.Entries.Add(typeof(float), () => new FloatOutputUi());
            OutputUiFactory.Entries.Add(typeof(int), () => new IntOutputUi());
            OutputUiFactory.Entries.Add(typeof(string), () => new StringOutputUi());
            OutputUiFactory.Entries.Add(typeof(Size2), () => new Size2OutputUi());
            OutputUiFactory.Entries.Add(typeof(ShaderResourceView), () => new ShaderResourceViewOutputUi());
            OutputUiFactory.Entries.Add(typeof(Texture2D), () => new Texture2dOutputUi());

            var symbols = SymbolRegistry.Entries;

            Load();

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

            // now load ui data
            SymbolChildUiRegistry.Load();
            InputUiRegistry.Load();
            OutputUiRegistry.Load();
        }

        public override void Save()
        {
            // first save core data
            base.Save();

            // save ui data
            SymbolChildUiRegistry.Save();
            InputUiRegistry.Save();
            OutputUiRegistry.Save();
        }

        public void CreateSymbolChildUisForInstance(Instance instance)
        {
            var uiEntries = SymbolChildUiRegistry.Entries;
            var symbol = instance.Symbol;
            var entriesForSymbol = uiEntries[symbol.Id];

            foreach (var child in instance.Children)
            {
                if (!entriesForSymbol.ContainsKey(child.Id))
                {
                    var childUi = new SymbolChildUi()
                                  {
                                      SymbolChild = symbol.Children.Find(c => c.Id == child.Id),
                                      PosOnCanvas = new Vector2(100, 100)
                                  };
                    uiEntries[symbol.Id].Add(child.Id, childUi);
                }

                CreateSymbolChildUisForInstance(child);
            }
        }


        public void CreateInputAndOutputUiEntriesForSymbol(Symbol symbol)
        {
            var inputDict = new Dictionary<Guid, IInputUi>();
            var inputUiFactory = InputUiFactory.Entries;
            foreach (var input in symbol.InputDefinitions)
            {
                var inputCreator = inputUiFactory[input.DefaultValue.ValueType];
                inputDict.Add(input.Id, inputCreator());
            }
            InputUiRegistry.Entries.Add(symbol.Id, inputDict);

            var outputDict = new Dictionary<Guid, IOutputUi>();
            var outputUiFactory = OutputUiFactory.Entries;
            foreach (var output in symbol.OutputDefinitions)
            {
                var outputUiCreator = outputUiFactory[output.ValueType];
                outputDict.Add(output.Id, outputUiCreator());
            }
            OutputUiRegistry.Entries.Add(symbol.Id, outputDict);
        }

        public void UpdateUiEntriesForSymbol(Symbol symbol)
        {
            if (!SymbolChildUiRegistry.Entries.ContainsKey(symbol.Id))
            {
                SymbolChildUiRegistry.Entries.Add(symbol.Id, new Dictionary<Guid, SymbolChildUi>());
            }

            var childUiEntries = SymbolChildUiRegistry.Entries[symbol.Id];
            foreach (var child in symbol.Children)
            {
                if (!childUiEntries.ContainsKey(child.Id))
                {
                    Log.Info($"Found no symbol child ui dictionary entry for symbol child '{child.ReadableName}' - creating a new one");
                    var childUi = new SymbolChildUi()
                                  {
                                      SymbolChild = child,
                                      PosOnCanvas = new Vector2(100, 100)
                                  };
                    childUiEntries.Add(child.Id, childUi);
                }
            }

            if (!InputUiRegistry.Entries.TryGetValue(symbol.Id, out var inputDict))
            {
                Log.Info($"Found no input ui dictionary entry for symbol '{symbol.Name}' - creating a new one");
                inputDict = new Dictionary<Guid, IInputUi>();
                InputUiRegistry.Entries.Add(symbol.Id, inputDict);
            }

            var inputUiFactory = InputUiFactory.Entries;
            foreach (var input in symbol.InputDefinitions)
            {
                if (!inputDict.TryGetValue(input.Id, out var value) || (value.Type != input.DefaultValue.ValueType))
                {
                    inputDict.Remove(input.Id);
                    var inputCreator = inputUiFactory[input.DefaultValue.ValueType];
                    inputDict.Add(input.Id, inputCreator());
                }
            }

            if (!OutputUiRegistry.Entries.TryGetValue(symbol.Id, out var outputDict))
            {
                Log.Info($"Found no output ui dictionary entry for symbol '{symbol.Name}' - creating a new one.");
                outputDict = new Dictionary<Guid, IOutputUi>();
                OutputUiRegistry.Entries.Add(symbol.Id, outputDict);
            }

            var outputUiFactory = OutputUiFactory.Entries;
            foreach (var output in symbol.OutputDefinitions)
            {
                if (!outputDict.TryGetValue(output.Id, out var value) || (value.Type != output.ValueType))
                {
                    outputDict.Remove(output.Id);
                    var outputUiCreator = outputUiFactory[output.ValueType];
                    outputDict.Add(output.Id, outputUiCreator());
                }
            }
        }

        public Instance MainOp;
    }
}