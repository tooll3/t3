using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Operator;
using T3.Core.Operator.Types;
using T3.Gui;

namespace T3
{
    public class MockModel
    {
        public MockModel()
        {
            Init();
        }

        private void Init()
        {
            // create and register input controls by type
            TypeUiRegistry.Entries.Add(typeof(float), new FloatUiProperties());
            TypeUiRegistry.Entries.Add(typeof(int), new IntUiProperties());
            TypeUiRegistry.Entries.Add(typeof(string), new StringUiProperties());

            // Register input ui creators
            InputUiFactory.Entries.Add(typeof(float), () => new FloatInputUi());
            InputUiFactory.Entries.Add(typeof(int), () => new IntInputUi());
            InputUiFactory.Entries.Add(typeof(string), () => new StringInputUi());

            // Register output ui creators
            OutputUiFactory.Entries.Add(typeof(float), () => new FloatOutputUi());
            OutputUiFactory.Entries.Add(typeof(int), () => new IntOutputUi());
            OutputUiFactory.Entries.Add(typeof(string), () => new StringOutputUi());

            var symbols = SymbolRegistry.Entries;
            var uiEntries = SymbolChildUiRegistry.Entries;

            // get types from assembly
            var asm = typeof(Symbol).Assembly;
            var instanceTypes = from type in asm.ExportedTypes
                                where type.IsSubclassOf(typeof(Instance))
                                where !type.IsGenericType
                                select type;

            foreach (var type in instanceTypes)
            {
                var symbol = new Symbol(type);
                symbols.Add(symbol.Id, symbol);
                CreateInputAndOutputUiEntriesForSymbol(symbol);
                uiEntries.Add(symbol.Id, new Dictionary<Guid, SymbolChildUi>());
            }

            var dashboardSymbol = symbols.First(entry => entry.Value.SymbolName == "Dashboard").Value;
            var projectSymbol = symbols.First(entry => entry.Value.SymbolName == "Project").Value;
            dashboardSymbol.Children.Add(new SymbolChild(projectSymbol));

            // create instance of project op, all children are create automatically
            var dashboard = dashboardSymbol.CreateInstance();
            Instance projectOp = dashboard.Children[0];

            // create ui data for project symbol
            uiEntries[dashboardSymbol.Id].Add(dashboardSymbol.Children[0].Id, new SymbolChildUi()
                                                                              {
                                                                                  SymbolChild = dashboardSymbol.Children[0],
                                                                                  Position = new Vector2(100, 100)
                                                                              });

            MainOp = projectOp;
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
            var inputDict = InputUiRegistry.Entries[symbol.Id];
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

            var outputDict = OutputUiRegistry.Entries[symbol.Id];
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