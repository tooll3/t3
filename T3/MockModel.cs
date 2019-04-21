using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using T3.Core.Operator;
using T3.Gui;
using Random = System.Random;

namespace T3
{
    class MockModel
    {
        public MockModel()
        {
            Init();
        }

        class Add : Instance<Add>
        {
            [OperatorAttribute(OperatorAttribute.OperatorType.Output)]
            public readonly Slot<float> Result = new Slot<float>();

            public Add()
            {
                Result.UpdateAction = Update;
            }

            private void Update(EvaluationContext context)
            {
                Result.Value = Input1.GetValue(context) + Input2.GetValue(context);
            }

            public readonly InputSlot<float> Input1 = new InputSlot<float>(5.0f);
            public readonly InputSlot<float> Input2 = new InputSlot<float>(10.0f);
        }

        class Random : Instance<Random>
        {
            [OperatorAttribute(OperatorAttribute.OperatorType.Output)]
            public readonly Slot<float> Result = new Slot<float>();

            public Random()
            {
                Result.UpdateAction = Update;
            }

            private void Update(EvaluationContext context)
            {
                var random = new System.Random(Seed.GetValue(context));
                Result.Value = (float)random.NextDouble();
            }

            public readonly InputSlot<int> Seed = new InputSlot<int>(0);
        }

        class FloatFormat : Instance<FloatFormat>
        {
            [OperatorAttribute(OperatorAttribute.OperatorType.Output)]
            public readonly Slot<string> Output = new Slot<string>();

            public FloatFormat()
            {
                Output.UpdateAction = Update;
            }

            private void Update(EvaluationContext context)
            {
                Output.Value = Input.GetValue(context).ToString();
            }

            public readonly InputSlot<float> Input = new InputSlot<float>(3.0f);
        }

        class StringLength : Instance<StringLength>
        {
            [OperatorAttribute(OperatorAttribute.OperatorType.Output)]
            public readonly Slot<int> Length = new Slot<int>();

            public StringLength()
            {
                Length.UpdateAction = Update;
            }

            private void Update(EvaluationContext context)
            {
                Length.Value = InputString.GetValue(context).Length;
            }

            public readonly InputSlot<string> InputString = new InputSlot<string>(string.Empty);
        }

        class StringConcat : Instance<StringConcat>
        {
            [OperatorAttribute(OperatorAttribute.OperatorType.Output)]
            public readonly Slot<string> Result = new Slot<string>();

            public StringConcat()
            {
                Result.UpdateAction = Update;
            }

            private void Update(EvaluationContext context)
            {
                Result.Value = Input1.GetValue(context) + Input2.GetValue(context);
            }

            public readonly InputSlot<string> Input1 = new InputSlot<string>(string.Empty);
            public readonly InputSlot<string> Input2 = new InputSlot<string>(string.Empty);
        }

        class Time : Instance<Time>
        {
            [OperatorAttribute(OperatorAttribute.OperatorType.Output)]
            public readonly Slot<float> TimeInSeconds = new Slot<float>();

            public Time()
            {
                TimeInSeconds.UpdateAction = Update;
                _watch.Start();
            }

            private void Update(EvaluationContext context)
            {
                TimeInSeconds.Value = _watch.ElapsedMilliseconds/1000.0f;
            }

            private Stopwatch _watch = new Stopwatch();
        }

        class Project : Instance<Project>
        {
            [OperatorAttribute(OperatorAttribute.OperatorType.Output)]
            public readonly Slot<string> Output = new Slot<string>("Project Output");



            public readonly InputSlot<float> Input = new InputSlot<float>(0.0f);
        }

        class Dashboard : Instance<Dashboard>
        {

        }

        private void Init()
        {
            var addSymbol = new Symbol(typeof(Add));
            addSymbol.InputDefinitions[0].DefaultValue = new InputValue<float>(5.0f);
            addSymbol.InputDefinitions[1].DefaultValue = new InputValue<float>(10.0f);

            var randomSymbol = new Symbol(typeof(Random));
            randomSymbol.InputDefinitions[0].DefaultValue = new InputValue<int>(42);

            var floatFormatSymbol = new Symbol(typeof(FloatFormat));
            floatFormatSymbol.InputDefinitions[0].DefaultValue = new InputValue<float>(1.0f);

            var stringLengthSymbol = new Symbol(typeof(StringLength));
            stringLengthSymbol.InputDefinitions[0].DefaultValue = new InputValue<string>(string.Empty);

            var stringConcatSymbol = new Symbol(typeof(StringConcat));
            stringConcatSymbol.InputDefinitions[0].DefaultValue = new InputValue<string>(string.Empty);
            stringConcatSymbol.InputDefinitions[1].DefaultValue = new InputValue<string>(string.Empty);

            var timeSymbol = new Symbol(typeof(Time));

            var projectSymbol = new Symbol(typeof(Project));
            projectSymbol.InputDefinitions[0].DefaultValue = new InputValue<float>(1.0f);
            projectSymbol.Children.AddRange(new[] { new SymbolChild(addSymbol), new SymbolChild(addSymbol), new SymbolChild(randomSymbol) });

            var dashboardSymbol = new Symbol(typeof(Dashboard))
                                  {
                                      Children =
                                      {
                                          new SymbolChild(projectSymbol),
                                      }
                                  };

            projectSymbol.AddConnection(new Symbol.Connection(sourceChildId: projectSymbol.Children[2].Id, // from Random
                                                              outputDefinitionId: randomSymbol.OutputDefinitions[0].Id,
                                                              targetChildId: projectSymbol.Children[0].Id, // to Add
                                                              inputDefinitionId: addSymbol.InputDefinitions[0].Id));

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
                                                     { stringConcatSymbol.OutputDefinitions[0].Id, new IntOutputUi() }
                                                 });

            // time
            outputUis.Add(timeSymbol.Id, new Dictionary<Guid, IOutputUi>()
                                         {
                                             { timeSymbol.OutputDefinitions[0].Id, new FloatOutputUi() }
                                         });

            // project
            inputUis.Add(projectSymbol.Id, new Dictionary<Guid, IInputUi>()
                                           {
                                               { projectSymbol.InputDefinitions[0].Id, new FloatInputUi { Position = Vector2.Zero } }
                                           });
            outputUis.Add(projectSymbol.Id, new Dictionary<Guid, IOutputUi>()
                                            {
                                                { projectSymbol.OutputDefinitions[0].Id, new StringOutputUi { Position = new Vector2(0.0f, 300.0f) } }
                                            });



            // create and register input controls by type
            InputUiRegistry.EntriesByType.Add(typeof(float), new FloatInputUi());
            InputUiRegistry.EntriesByType.Add(typeof(int), new IntInputUi());
            InputUiRegistry.EntriesByType.Add(typeof(string), new StringInputUi());
            OutputUiRegistry.EntriesByType.Add(typeof(float), new FloatOutputUi());
            OutputUiRegistry.EntriesByType.Add(typeof(int), new IntOutputUi());
            OutputUiRegistry.EntriesByType.Add(typeof(string), new StringOutputUi());

            _initialized = true;
            MainOp = projectOp;

            //_nodes.Add(new Node()
            //{
            //    ID = 0,
            //    Name = "MainTex",
            //    Pos = new Vector2(40, 50),
            //    Value = 0.5f,
            //    Color = TColors.White,
            //    InputsCount = 1,
            //    OutputsCount = 1,
            //}
            //);
            //_nodes.Add(new Node()
            //{
            //    ID = 1,
            //    Name = "MainTex2",
            //    Pos = new Vector2(140, 50),
            //    Value = 0.5f,
            //    Color = TColors.White,
            //    InputsCount = 1,
            //    OutputsCount = 1
            //}
            //);
            //_nodes.Add(new Node()
            //{
            //    ID = 2,
            //    Name = "MainTex3",
            //    Pos = new Vector2(240, 50),
            //    Value = 0.5f,
            //    Color = TColors.White,
            //    InputsCount = 1,
            //    OutputsCount = 1
            //}
            //);

            //_links.Add(new NodeLink(0, 0, 2, 0));
            //_links.Add(new NodeLink(1, 0, 2, 1));
        }

        public Instance MainOp;
        private bool _initialized;
    }
}