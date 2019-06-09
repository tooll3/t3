using System;
using System.Collections.Generic;
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

        class Dashboard : Instance<Dashboard>
        {

        }

        private void Init()
        {
            var addSymbol = new Symbol(typeof(Add));
            var randomSymbol = new Symbol(typeof(Core.Operator.Types.Random));
            var floatFormatSymbol = new Symbol(typeof(FloatFormat));
            var stringLengthSymbol = new Symbol(typeof(StringLength));
            var stringConcatSymbol = new Symbol(typeof(StringConcat));
            var timeSymbol = new Symbol(typeof(Time));

            var projectSymbol = new Symbol(typeof(Project));
            projectSymbol.Children.AddRange(new[] { new SymbolChild(addSymbol), new SymbolChild(addSymbol), new SymbolChild(randomSymbol) });

            var dashboardSymbol = new Symbol(typeof(Dashboard));
            dashboardSymbol.Children.Add(new SymbolChild(projectSymbol));

            projectSymbol.AddConnection(new Symbol.Connection(sourceChildId: projectSymbol.Children[2].Id, // from Random
                                                              sourceDefinitionId: randomSymbol.OutputDefinitions[0].Id,
                                                              targetChildId: projectSymbol.Children[0].Id, // to Add
                                                              targetDefinitionId: addSymbol.InputDefinitions[0].Id));

            // register the symbols globally
            var symbols = SymbolRegistry.Entries;
            symbols.Add(addSymbol.Id, addSymbol);
            symbols.Add(randomSymbol.Id, randomSymbol);
            symbols.Add(floatFormatSymbol.Id, floatFormatSymbol);
            symbols.Add(stringLengthSymbol.Id, stringLengthSymbol);
            symbols.Add(stringConcatSymbol.Id, stringConcatSymbol);
            symbols.Add(timeSymbol.Id, timeSymbol);
            symbols.Add(projectSymbol.Id, projectSymbol);
            symbols.Add(dashboardSymbol.Id, dashboardSymbol);

            // create instance of project op, all children are create automatically
            var dashboard = dashboardSymbol.CreateInstance();
            Instance projectOp = dashboard.Children[0];

            // create ui data for project symbol
            var uiEntries = SymbolChildUiRegistry.Entries;
            uiEntries.Add(addSymbol.Id, new Dictionary<Guid, SymbolChildUi>());
            uiEntries.Add(randomSymbol.Id, new Dictionary<Guid, SymbolChildUi>());
            uiEntries.Add(floatFormatSymbol.Id, new Dictionary<Guid, SymbolChildUi>());
            uiEntries.Add(stringLengthSymbol.Id, new Dictionary<Guid, SymbolChildUi>());
            uiEntries.Add(stringConcatSymbol.Id, new Dictionary<Guid, SymbolChildUi>());
            uiEntries.Add(timeSymbol.Id, new Dictionary<Guid, SymbolChildUi>());
            uiEntries.Add(projectSymbol.Id, new Dictionary<Guid, SymbolChildUi>()
                                            {
                                                {
                                                    projectOp.Children[0].Id, new SymbolChildUi
                                                                              {
                                                                                  SymbolChild = projectSymbol.Children[0],
                                                                                  Position = new Vector2(100, 100)
                                                                              }
                                                },
                                                {
                                                    projectOp.Children[1].Id, new SymbolChildUi
                                                                              {
                                                                                  SymbolChild = projectSymbol.Children[1],
                                                                                  Position = new Vector2(50, 200)
                                                                              }
                                                },
                                                {
                                                    projectOp.Children[2].Id, new SymbolChildUi
                                                                              {
                                                                                  SymbolChild = projectSymbol.Children[2],
                                                                                  Position = new Vector2(250, 200)
                                                                              }
                                                },
                                            });
            uiEntries.Add(dashboardSymbol.Id, new Dictionary<Guid, SymbolChildUi>()
                                              {
                                                  {
                                                      dashboardSymbol.Children[0].Id, new SymbolChildUi()
                                                                                      {
                                                                                          SymbolChild = dashboardSymbol.Children[0],
                                                                                          Position = new Vector2(100, 100)
                                                                                      }
                                                  }
                                              });

            // Register input ui creators
            InputUiFactory.Entries.Add(typeof(float), () => new FloatInputUi());
            InputUiFactory.Entries.Add(typeof(int), () => new IntInputUi());
            InputUiFactory.Entries.Add(typeof(string), () => new StringInputUi());

            // Register output ui creators
            OutputUiFactory.Entries.Add(typeof(float), () => new FloatOutputUi());
            OutputUiFactory.Entries.Add(typeof(int), () => new IntOutputUi());
            OutputUiFactory.Entries.Add(typeof(string), () => new StringOutputUi());

            CreateUiEntriesForSymbol(addSymbol);
            CreateUiEntriesForSymbol(randomSymbol);
            CreateUiEntriesForSymbol(floatFormatSymbol);
            CreateUiEntriesForSymbol(stringLengthSymbol);
            CreateUiEntriesForSymbol(stringConcatSymbol);
            CreateUiEntriesForSymbol(timeSymbol);
            CreateUiEntriesForSymbol(projectSymbol);
            CreateUiEntriesForSymbol(dashboardSymbol);

            // create and register input controls by type
            InputUiRegistry.EntriesByType.Add(typeof(float), new FloatInputUi());
            InputUiRegistry.EntriesByType.Add(typeof(int), new IntInputUi());
            InputUiRegistry.EntriesByType.Add(typeof(string), new StringInputUi());

            MainOp = projectOp;
        }

        public void CreateUiEntriesForSymbol(Symbol symbol)
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
            InputUiRegistry.Entries.Remove(symbol.Id);
            OutputUiRegistry.Entries.Remove(symbol.Id);
            CreateUiEntriesForSymbol(symbol);
        }

        public Instance MainOp;
    }
}