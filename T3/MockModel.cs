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
            Symbol _cubeSymbol = new Symbol()
            {
                Id = Guid.NewGuid(),
                SymbolName = "Cube",
            };

            Symbol _groupSymbol = new Symbol()
            {
                Id = Guid.NewGuid(),
                SymbolName = "Group",
            };

            Symbol _exampleProject = new Symbol()
            {
                Id = Guid.NewGuid(),
                _children = {
                    new InstanceDefinition()
                    {
                        InstanceId = Guid.NewGuid(),

                        Symbol = _cubeSymbol,
                    },
                    new InstanceDefinition()
                    {
                        InstanceId = Guid.NewGuid(),
                        Symbol = _cubeSymbol,
                    },
                    new InstanceDefinition()
                    {
                        InstanceId = Guid.NewGuid(),
                        Symbol = _groupSymbol,
                    },
                }
            };

            var symbols = SymbolRegistry.Instance.Definitions;
            symbols.Add(_cubeSymbol.Id, _cubeSymbol);
            symbols.Add(_groupSymbol.Id, _groupSymbol);
            symbols.Add(_exampleProject.Id, _exampleProject);

            Instance _projectOp = new Instance()
            {
                Parent = null,
                Symbol = _exampleProject,
            };
            _exampleProject._instancesOfSymbol.Add(_projectOp);

            _projectOp.Children = new List<Instance>(){
                 new Instance()
                 {
                     Parent = _projectOp,
                     Symbol = _cubeSymbol,
                     Id = _exampleProject._children[0].InstanceId,
                     //InstanceDefinition = _exampleProject._children[0],
                 },
                 new Instance()
                 {
                     Parent = _projectOp,
                     Symbol = _cubeSymbol,
                     Id= _exampleProject._children[1].InstanceId,
                     //InstanceDefinition = _exampleProject._children[1],
                 },
                new Instance()
                 {
                     Parent = _projectOp,
                     Symbol = _groupSymbol,
                     Id = _exampleProject._children[2].InstanceId,
                 },
            };
            _cubeSymbol._instancesOfSymbol.Add(_projectOp.Children[0]);
            _cubeSymbol._instancesOfSymbol.Add(_projectOp.Children[1]);
            _groupSymbol._instancesOfSymbol.Add(_projectOp.Children[2]);

            var uiEntries = InstanceUiRegistry.Instance.UiEntries;
            uiEntries.Add(_exampleProject.Id, new Dictionary<Guid, InstanceUi>()
                {
                    {_projectOp.Children[0].Id, new InstanceUi() { Instance =_projectOp.Children[0], Name="Cube1" } },
                    {_projectOp.Children[1].Id, new InstanceUi() { Instance =_projectOp.Children[1] } },
                    {_projectOp.Children[2].Id, new InstanceUi() { Instance =_projectOp.Children[2], Name="MyGroup" } },
                });

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
            _initialized = true;
            MainOp = _projectOp;
        }

        public Instance MainOp;
        private bool _initialized;
    }
}
