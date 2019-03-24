using System;
using System.Collections.Generic;
using T3.Core.Operator;

namespace T3
{
    class MockModel
    {
        public MockModel()
        {
            Init();
        }

        private void Init()
        {
            var cubeSymbol = new Symbol() { Id = Guid.NewGuid(), SymbolName = "Cube", };
            var groupSymbol = new Symbol() { Id = Guid.NewGuid(), SymbolName = "Group", };
            var projectSymbol = new Symbol()
                                {
                                    Id = Guid.NewGuid(),
                                    _children =
                                    {
                                        new SymbolChild() { InstanceId = Guid.NewGuid(), Symbol = cubeSymbol, },
                                        new SymbolChild() { InstanceId = Guid.NewGuid(), Symbol = cubeSymbol, },
                                        new SymbolChild() { InstanceId = Guid.NewGuid(), Symbol = groupSymbol },
                                    }
                                };

            // register the symbols globally
            var symbols = SymbolRegistry.Instance.Definitions;
            symbols.Add(cubeSymbol.Id, cubeSymbol);
            symbols.Add(groupSymbol.Id, groupSymbol);
            symbols.Add(projectSymbol.Id, projectSymbol);

            // create instance of project op, all children are create automatically
            Instance projectOp = projectSymbol.CreateInstance();

            // create ui data for project symbol
            var uiEntries = InstanceUiRegistry.Instance.UiEntries;
            uiEntries.Add(projectSymbol.Id, new Dictionary<Guid, InstanceUi>()
                                            {
                                                { projectOp.Children[0].Id, new InstanceUi { Instance = projectOp.Children[0], Name = "Cube1" } },
                                                { projectOp.Children[1].Id, new InstanceUi { Instance = projectOp.Children[1] } },
                                                { projectOp.Children[2].Id, new InstanceUi { Instance = projectOp.Children[2], Name = "MyGroup" } },
                                            });
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
