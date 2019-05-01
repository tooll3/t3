using System;
using System.Collections.Generic;
using System.Numerics;
using T3.Core.Operator;
using T3.Core.Operator.Types;
using T3.Gui;

namespace T3
{
    class MockModel
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
                                                                                  Name = "Add1",
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
                                                                                  Name = "Random",
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
                                                                                          Name = "Project Op",
                                                                                          Position = new Vector2(100, 100)
                                                                                      }
                                                  }
                                              });

            var outputUis = OutputUiRegistry.Entries;
            var inputUis = InputUiRegistry.Entries;
            // add
            inputUis.Add(addSymbol.Id, new Dictionary<Guid, IInputUi>()
                                       {
                                           { addSymbol.InputDefinitions[0].Id, new FloatInputUi() },
                                           { addSymbol.InputDefinitions[1].Id, new FloatInputUi() }
                                       });
            outputUis.Add(addSymbol.Id, new Dictionary<Guid, IOutputUi>()
                                        {
                                            { addSymbol.OutputDefinitions[0].Id, new FloatOutputUi() },
                                        });

            // random
            inputUis.Add(randomSymbol.Id, new Dictionary<Guid, IInputUi>()
                                          {
                                              { randomSymbol.InputDefinitions[0].Id, new IntInputUi() }
                                          });
            outputUis.Add(randomSymbol.Id, new Dictionary<Guid, IOutputUi>()
                                           {
                                               { randomSymbol.OutputDefinitions[0].Id, new FloatOutputUi() }
                                           });

            // float format
            inputUis.Add(floatFormatSymbol.Id, new Dictionary<Guid, IInputUi>()
                                               {
                                                   { floatFormatSymbol.InputDefinitions[0].Id, new FloatInputUi() }
                                               });
            outputUis.Add(floatFormatSymbol.Id, new Dictionary<Guid, IOutputUi>()
                                                {
                                                    { floatFormatSymbol.OutputDefinitions[0].Id, new StringOutputUi() }
                                                });

            // string length
            inputUis.Add(stringLengthSymbol.Id, new Dictionary<Guid, IInputUi>()
                                                {
                                                    { stringLengthSymbol.InputDefinitions[0].Id, new StringInputUi() }
                                                });
            outputUis.Add(stringLengthSymbol.Id, new Dictionary<Guid, IOutputUi>()
                                                 {
                                                     { stringLengthSymbol.OutputDefinitions[0].Id, new IntOutputUi() }
                                                 });

            // string concat
            inputUis.Add(stringConcatSymbol.Id, new Dictionary<Guid, IInputUi>()
                                                {
                                                    { stringConcatSymbol.InputDefinitions[0].Id, new StringInputUi() },
                                                    { stringConcatSymbol.InputDefinitions[1].Id, new StringInputUi() }
                                                });
            outputUis.Add(stringConcatSymbol.Id, new Dictionary<Guid, IOutputUi>()
                                                 {
                                                     { stringConcatSymbol.OutputDefinitions[0].Id, new StringOutputUi() }
                                                 });

            // time
            outputUis.Add(timeSymbol.Id, new Dictionary<Guid, IOutputUi>()
                                         {
                                             { timeSymbol.OutputDefinitions[0].Id, new FloatOutputUi() }
                                         });

            // project
            inputUis.Add(projectSymbol.Id, new Dictionary<Guid, IInputUi>()
                                           {
                                               { projectSymbol.InputDefinitions[0].Id, new FloatInputUi { Position = new Vector2(40.0f, 300.0f) } }
                                           });
            outputUis.Add(projectSymbol.Id, new Dictionary<Guid, IOutputUi>()
                                            {
                                                { projectSymbol.OutputDefinitions[0].Id, new StringOutputUi { Position = new Vector2(40.0f, 0.0f) } }
                                            });

            // dashboard
            inputUis.Add(dashboardSymbol.Id, new Dictionary<Guid, IInputUi>());
            outputUis.Add(dashboardSymbol.Id, new Dictionary<Guid, IOutputUi>());


            // create and register input controls by type
            InputUiRegistry.EntriesByType.Add(typeof(float), new FloatInputUi());
            InputUiRegistry.EntriesByType.Add(typeof(int), new IntInputUi());
            InputUiRegistry.EntriesByType.Add(typeof(string), new StringInputUi());

            MainOp = projectOp;
        }

        public Instance MainOp;
    }
}