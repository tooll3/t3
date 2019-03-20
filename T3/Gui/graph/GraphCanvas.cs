using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using T3.Core.Operator;

namespace t3.graph
{
    /// <summary>
    /// A mock implementation of a future graph renderer
    /// </summary>
    class GraphCanvas
    {
        public void Draw(ref bool opened)
        {
            if (!ImGui.Begin("Example", ref opened)) { ImGui.End(); return; }
            {
                if (!_initialized)
                    Init();

                _drawList = ImGui.GetWindowDrawList();
                _overlayDrawList = ImGui.GetOverlayDrawList();

                DrawNodeList();
                ImGui.SameLine();
                DrawCanvas();
            }
            ImGui.End();
        }





        private void DrawNodeList()
        {
            // Draw a list of nodes on the left side
            ImGui.BeginChild("node_list", new Vector2(80, 0));
            {
                ImGui.Text("Nodes");
                ImGui.Separator();

                var symbol = _mainOp.Symbol;
                //var allUiEntriesForChildrenOfSymbol = InstanceUiRegistry.Instance.UiEntries[symbol.Id];
                //var uiEntryForASpecificInstance = allUiEntriesForChildrenOfSymbol[instance.Id];


                foreach (var pair in InstanceUiRegistry.Instance.UiEntries[symbol.Id])
                {
                    var instanceUi = pair.Value;
                    ImGui.PushID(pair.Key.ToString());
                    {
                        var name = symbol._children.Find(entry => entry.InstanceId == pair.Key).ReadableName;
                        if (ImGui.Selectable(name, ref instanceUi.Selected))
                        {
                            // Change selection 
                            foreach (var nodePair in InstanceUiRegistry.Instance.UiEntries[symbol.Id])
                            {
                                var node2 = nodePair.Value;
                                if (node2.Selected && node2 != instanceUi)
                                {
                                    node2.Selected = false;
                                }
                            }
                        }
                    }
                    ImGui.PopID();
                }
            }
            ImGui.EndChild();
        }


        public void StartLinkFromInput(Node nodeWithInput, int inputSlotIndex)
        {
            _linkUnderConstruction = new NodeLink()
            {
                OutputNodeIndex = -1,
                OutputSlotIndex = -1,
                InputSlotIndex = inputSlotIndex,
                InputNodeIndex = nodeWithInput.ID,
            };
        }


        public void StartLinkFromOutput(Node nodeWithOutput, int outputSlotIndex)
        {
            _linkUnderConstruction = new NodeLink()
            {
                OutputSlotIndex = outputSlotIndex,
                OutputNodeIndex = nodeWithOutput.ID,
                InputNodeIndex = -1,
                InputSlotIndex = -1,
            };
        }

        public void CompleteLinkToOutput(Node nodeWithOutput, int outputSlotIndex)
        {
            //if (_linkUnderConstruction == null)
            //    return;

            //_linkUnderConstruction.OutputSlotIndex = outputSlotIndex;
            //_linkUnderConstruction.OutputNodeIndex = nodeWithOutput.ID;
            //_links.Add(_linkUnderConstruction);
            //_linkUnderConstruction = null;
        }

        public void CompleteLinkToInput(Node nodeWithInput, int inputSlotIndex)
        {
            //if (_linkUnderConstruction == null)
            //    return;

            //_linkUnderConstruction.InputSlotIndex = inputSlotIndex;
            //_linkUnderConstruction.InputNodeIndex = nodeWithInput.ID;
            //_links.Add(_linkUnderConstruction);
            //_linkUnderConstruction = null;
        }


        public void CancelLink()
        {
            _linkUnderConstruction = null;
        }


        private void DrawCanvas()
        {

            //ImGui.BeginGroup();
            //{
            //    ImGui.Text(_debugMessages); _debugMessages = "";

            //    _mouse = ImGui.GetMousePos();
            //    _io = ImGui.GetIO();
            //    ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(1, 1));
            //    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            //    ImGui.PushStyleColor(ImGuiCol.WindowBg, TColors.ToUint(60, 60, 70, 200));

            //    // Damp scaling
            //    _scale = Im.Lerp(_scale, _scaleTarget, _io.DeltaTime * 20);
            //    _scroll = Im.Lerp(_scroll, _scrollTarget, _io.DeltaTime * 20);

            //    THelpers.DebugWindowRect("window");
            //    ImGui.BeginChild("scrolling_region", new Vector2(0, 0), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
            //    {
            //        THelpers.DebugWindowRect("window.scrollingRegion");
            //        _canvasPos = ImGui.GetWindowPos();
            //        _size = ImGui.GetWindowSize();
            //        _drawList.PushClipRect(_canvasPos, _canvasPos + _size);

            //        // Canvas interaction --------------
            //        if (ImGui.IsWindowHovered())
            //        {
            //            if (ImGui.IsMouseDragging(1))
            //            {
            //                _scrollTarget += _io.MouseDelta;
            //            }

            //            // Zoom with mouse wheel
            //            if (_io.MouseWheel != 0)
            //            {
            //                const float zoomSpeed = 1.2f;
            //                var focusCenter = (_mouse - _scroll - _canvasPos) / _scale;

            //                _overlayDrawList.AddCircle(focusCenter + ImGui.GetWindowPos(), 10, Color.Red.ToUint());

            //                if (_io.MouseWheel < 0.0f)
            //                {
            //                    _scaleTarget = Im.Max(0.3f, _scaleTarget / zoomSpeed);
            //                }

            //                if (_io.MouseWheel > 0.0f)
            //                {
            //                    _scaleTarget = Im.Min(3.0f, _scaleTarget * zoomSpeed);
            //                }

            //                Vector2 shift = _scrollTarget + (focusCenter * _scaleTarget);
            //                _scrollTarget += _mouse - shift - _canvasPos;
            //            }

            //            ImGui.SetScrollY(0);    // HACK: prevent jump of scroll position by accidental scrolling
            //        }

            //        // Draw Grid ------------
            //        {
            //            var gridSize = 64.0f * _scale;
            //            for (float x = _scroll.X % gridSize; x < _size.X; x += gridSize)
            //            {
            //                _drawList.AddLine(
            //                    new Vector2(x, 0.0f) + _canvasPos,
            //                    new Vector2(x, _size.Y) + _canvasPos,
            //                    new Color(0.5f, 0.5f, 0.5f, 0.1f).ToUint());
            //            }

            //            for (float y = _scroll.Y % gridSize; y < _size.Y; y += gridSize)
            //            {
            //                _drawList.AddLine(
            //                    new Vector2(0.0f, y) + _canvasPos,
            //                    new Vector2(_size.X, y) + _canvasPos,
            //                    new Color(0.5f, 0.5f, 0.5f, 0.1f).ToUint());
            //            }
            //        }

            //        // Draw links
            //        DrawLinks();

            //        // Draw nodes
            //        foreach (var node in _nodes)
            //        {
            //            GraphNode.DrawOnCanvas(node, this);
            //        }

            //        _debugMessages += ImGui.IsAnyItemHovered() ? "anyItemHovered " : "";
            //        _debugMessages += ImGui.IsWindowHovered() ? "isWindowHovered " : "";
            //        _debugMessages += ImGui.IsItemHovered() ? "isWindowHovered " : "";


            //        _drawList.PopClipRect();
            //    }
            //    ImGui.EndChild();
            //    ImGui.PopStyleColor();
            //    ImGui.PopStyleVar(2);
            //}
            //ImGui.EndGroup();
        }

        private void DrawLinks()
        {
            //foreach (var link in _links)
            //{
            //    var node_inp = _nodes[link.OutputNodeIndex];
            //    var node_out = _nodes[link.InputNodeIndex];
            //    var p1 = GetScreenPosFrom(node_inp.GetOutputSlotPos(link.OutputSlotIndex));
            //    var p2 = GetScreenPosFrom(node_out.GetInputSlotPos(link.InputSlotIndex));
            //    _drawList.AddBezierCurve(p1, p1 + new Vector2(+50, 0), p2 + new Vector2(-50, 0), p2, TColors.ToUint(200, 200, 100, 255), 3.0f);
            //}

            //// Draw Link under construction
            //if (_linkUnderConstruction != null)
            //{
            //    if (ImGui.IsMouseReleased(0))
            //    {
            //        //_linkUnderConstruction = null;
            //        return;
            //    }
            //    var luc = _linkUnderConstruction;
            //    var node_inp = luc.OutputNodeIndex == -1 ? null : _nodes[luc.OutputNodeIndex];
            //    var node_out = luc.InputNodeIndex == -1 ? null : _nodes[luc.InputNodeIndex];

            //    var p1 = node_inp == null ? ImGui.GetMousePos() : GetScreenPosFrom(node_inp.GetOutputSlotPos(luc.OutputSlotIndex));
            //    var p2 = node_out == null ? ImGui.GetMousePos() : GetScreenPosFrom(node_out.GetInputSlotPos(luc.InputSlotIndex));
            //    _drawList.AddBezierCurve(p1, p1 + new Vector2(+50, 0), p2 + new Vector2(-50, 0), p2, TColors.ToUint(200, 200, 100, 255), 3.0f);
            //}
        }

        public void MoveSelected(Vector2 delta)
        {
            //foreach (var node in _nodes)
            //{
            //    if (!node.IsSelected)
            //        continue;

            //    node.Pos += delta;
            //}
        }


        /// <summary>
        /// Get screen position applying canas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public Vector2 GetScreenPosFrom(Vector2 posOnCanvas)
        {
            return posOnCanvas * _scale + _scroll + _canvasPos;
        }

        /// <summary>
        /// Get relative position with canvas by applying zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public Vector2 GetChildPosFrom(Vector2 posOnCanvas)
        {
            return posOnCanvas * _scale + _scroll;
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
                        Name="Cube1",
                        Symbol = _cubeSymbol,
                    },
                    new InstanceDefinition()
                    {
                        InstanceId = Guid.NewGuid(),
                        Name="",
                        Symbol = _cubeSymbol,
                    },
                    new InstanceDefinition()
                    {
                        InstanceId = Guid.NewGuid(),
                        Name="Group",
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
                 },
                 new Instance()
                 {
                     Parent = _projectOp,
                     Symbol = _cubeSymbol,
                     Id = _exampleProject._children[1].InstanceId,
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
                    {_projectOp.Children[0].Id, new InstanceUi() { Instance =_projectOp.Children[0] } },
                    {_projectOp.Children[1].Id, new InstanceUi() { Instance =_projectOp.Children[1] } },
                    {_projectOp.Children[2].Id, new InstanceUi() { Instance =_projectOp.Children[2] } },
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
            _mainOp = _projectOp;
        }

        private NodeLink _linkUnderConstruction;

        Instance _mainOp;

        bool _contextMenuOpened = false;

        //List<Node> _nodes = new List<Node>();
        //List<NodeLink> _links = new List<NodeLink>();

        bool _initialized = false;
        ImDrawListPtr _overlayDrawList;
        Vector2 _size;
        Vector2 _mouse;

        public ImDrawListPtr _drawList;
        private Vector2 _scroll = new Vector2(0.0f, 0.0f);
        private Vector2 _scrollTarget = new Vector2(0.0f, 0.0f);

        public Vector2 _canvasPos;
        public float _scale = 1; //the damped scale factor {read only}
        float _scaleTarget = 1;

        string _debugMessages = "";
        ImGuiIOPtr _io;
    }
}