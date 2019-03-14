using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using imHelpers;
using t3.iuhelpers;

namespace t3.graph
{
    /// <summary>
    /// A mock implementation of a future graph renderer
    /// </summary>
    class GraphCanvas
    {
        bool _contextMenuOpened = false;

        List<Node> _nodes = new List<Node>();
        List<NodeLink> _links = new List<NodeLink>();

        bool _initialized = false;
        bool _gridVisible = true;
        ImDrawListPtr _overlayDrawList;
        Vector2 _size;
        Vector2 _mouse;

        public ImDrawListPtr _drawList;
        private Vector2 _scroll = new Vector2(0.0f, 0.0f);
        public Vector2 _canvasPos;
        public float _scale = 1; //the damped scale factor {read only}
        float _scaleTarget = 1;

        bool _debugFlag;
        string _debugMessages = "";
        ImGuiIOPtr _io;


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
                foreach (var node in _nodes)
                {
                    ImGui.PushID(node.ID);
                    {
                        if (ImGui.Selectable(node.Name, ref node.IsSelected))
                        {
                            foreach (var node2 in _nodes)
                            {
                                if (node2.IsSelected && node2 != node)
                                {
                                    node2.IsSelected = false;
                                }
                            }
                        }
                    }
                    ImGui.PopID();
                }
            }
            ImGui.EndChild();
        }

        private NodeLink _linkUnderConstruction;

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
            if (_linkUnderConstruction == null)
                return;

            _linkUnderConstruction.OutputSlotIndex = outputSlotIndex;
            _linkUnderConstruction.OutputNodeIndex = nodeWithOutput.ID;
            _links.Add(_linkUnderConstruction);
            _linkUnderConstruction = null;
        }

        public void CompleteLinkToInput(Node nodeWithInput, int inputSlotIndex)
        {
            if (_linkUnderConstruction == null)
                return;

            _linkUnderConstruction.InputSlotIndex = inputSlotIndex;
            _linkUnderConstruction.InputNodeIndex = nodeWithInput.ID;
            _links.Add(_linkUnderConstruction);
            _linkUnderConstruction = null;
        }


        public void CancelLink()
        {
            _linkUnderConstruction = null;
        }


        private void DrawCanvas()
        {
            ImGui.BeginGroup();
            {
                ImGui.Text(_debugMessages); _debugMessages = "";

                _mouse = ImGui.GetMousePos();
                _io = ImGui.GetIO();
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(1, 1));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
                ImGui.PushStyleColor(ImGuiCol.WindowBg, TColors.ToUint(60, 60, 70, 200));

                _scale = Im.Lerp(_scale, _scaleTarget, _io.DeltaTime * 5);

                THelpers.DebugWindowRect("window");
                ImGui.BeginChild("scrolling_region", new Vector2(0, 0), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
                {
                    THelpers.DebugWindowRect("window.scrollingRegion");
                    _canvasPos = ImGui.GetWindowPos();
                    _size = ImGui.GetWindowSize();
                    _drawList.PushClipRect(_canvasPos, _canvasPos + _size);

                    // Canvas interaction --------------
                    if (ImGui.IsWindowHovered())
                    {
                        if (ImGui.IsMouseDragging(0))
                            _scroll += _io.MouseDelta;

                        // Zoom with mouse wheel
                        {
                            Vector2 focusCenter = (_mouse - _scroll - _canvasPos) / _scale;
                            _overlayDrawList.AddCircle(focusCenter + ImGui.GetWindowPos(), 10, Color.Red.ToUint());

                            if (_io.MouseWheel < 0.0f)
                                for (float zoom = _io.MouseWheel; zoom < 0.0f; zoom += 1.0f)
                                    _scaleTarget = Im.Max(0.3f, _scaleTarget / 1.05f);

                            if (_io.MouseWheel > 0.0f)
                                for (float zoom = _io.MouseWheel; zoom > 0.0f; zoom -= 1.0f)
                                    _scaleTarget = Im.Min(3.0f, _scaleTarget * 1.05f);

                            Vector2 shift = _scroll + (focusCenter * _scale);
                            _scroll += _mouse - shift - _canvasPos;

                            ImGui.SetScrollY(0);    // HACK: prevent jump of scroll position by accidental scrolling
                        }

                        // if (ImGui.IsMouseReleased(1))
                        //     if (_io.MouseDragMaxDistanceSqr[1] < (_io.MouseDragThreshold * _io.MouseDragThreshold))
                        //         ImGui.OpenPopup("NodesContextMenu");
                    }

                    // Draw Grid ------------
                    {
                        var gridSize = 64.0f * _scale;
                        for (float x = _scroll.X % gridSize; x < _size.X; x += gridSize)
                        {
                            _drawList.AddLine(
                                new Vector2(x, 0.0f) + _canvasPos,
                                new Vector2(x, _size.Y) + _canvasPos,
                                new Color(0.5f, 0.5f, 0.5f, 0.1f).ToUint());
                        }

                        for (float y = _scroll.Y % gridSize; y < _size.Y; y += gridSize)
                        {
                            _drawList.AddLine(
                                new Vector2(0.0f, y) + _canvasPos,
                                new Vector2(_size.X, y) + _canvasPos,
                                new Color(0.5f, 0.5f, 0.5f, 0.1f).ToUint());
                        }
                    }

                    // Draw links
                    DrawLinks();

                    // ImGui.PushItemWidth(120.0f);
                    // Draw nodes
                    foreach (var node in _nodes)
                    {
                        GraphNode.DrawOnCanvas(node, this);
                    }

                    THelpers.DebugItemRect("Last Node");
                    THelpers.DebugRect(ImGui.GetWindowContentRegionMin(), ImGui.GetWindowContentRegionMax(), "contentRegion");

                    _debugMessages += ImGui.IsAnyItemHovered() ? "anyItemHovered " : "";
                    _debugMessages += ImGui.IsWindowHovered() ? "isWindowHovered " : "";
                    _debugMessages += ImGui.IsItemHovered() ? "isWindowHovered " : "";


                    // // Open context menu
                    // if (!ImGui.IsAnyItemHovered() && ImGui.IsWindowHovered() && ImGui.IsMouseClicked(1))
                    // {
                    //     _selectedNodeID = _hoveredListNodeIndex = _hoveredSceneNodeIndex = -1;
                    //     _contextMenuOpened = true;
                    // }

                    // if (_contextMenuOpened)
                    // {
                    //     ImGui.OpenPopup("context_menu");
                    //     if (_hoveredListNodeIndex != -1)
                    //         _selectedNodeID = _hoveredListNodeIndex;

                    //     if (_hoveredSceneNodeIndex != -1)
                    //         _selectedNodeID = _hoveredSceneNodeIndex;
                    // }

                    // // Draw context menu
                    // ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
                    // if (ImGui.BeginPopup("context_menu"))
                    // {
                    //     Vector2 scene_pos = ImGui.GetMousePosOnOpeningCurrentPopup() - scrollOffset;
                    //     var isANodeSelected = _selectedNodeID != -1;
                    //     if (isANodeSelected)
                    //     {
                    //         var node = _nodes[_selectedNodeID];
                    //         ImGui.Text("Node '{node.Name}'");
                    //         ImGui.Separator();
                    //         if (ImGui.MenuItem("Rename..", null, false, false)) { }
                    //         if (ImGui.MenuItem("Delete", null, false, false)) { }
                    //         if (ImGui.MenuItem("Copy", null, false, false)) { }
                    //     }
                    //     else
                    //     {
                    //         if (ImGui.MenuItem("Add")) { _nodes.Add(new Node(_nodes.Count, "New node", scene_pos, 0.5f, new Vector4(0.5f, 0.5f, 0.5f, 1), 2, 2)); }
                    //         if (ImGui.MenuItem("Paste", null, false, false)) { }
                    //     }
                    //     ImGui.EndPopup();
                    // }
                    // ImGui.PopStyleVar();

                    // Scrolling
                    // if (ImGui.IsWindowHovered() && !ImGui.IsAnyItemActive() && ImGui.IsMouseDragging(2, 0.0f))
                    //     _scroll = _scroll + ImGui.GetIO().MouseDelta;

                    // ImGui.PopItemWidth();
                    _drawList.PopClipRect();
                }
                ImGui.EndChild();
                ImGui.PopStyleColor();
                ImGui.PopStyleVar(2);
            }
            ImGui.EndGroup();
        }

        private void DrawLinks()
        {
            foreach (var link in _links)
            {
                var node_inp = _nodes[link.OutputNodeIndex];
                var node_out = _nodes[link.InputNodeIndex];
                var p1 = GetScreenPosFrom(node_inp.GetOutputSlotPos(link.OutputSlotIndex));
                var p2 = GetScreenPosFrom(node_out.GetInputSlotPos(link.InputSlotIndex));
                _drawList.AddBezierCurve(p1, p1 + new Vector2(+50, 0), p2 + new Vector2(-50, 0), p2, TColors.ToUint(200, 200, 100, 255), 3.0f);
            }

            // Draw Link under construction
            if (_linkUnderConstruction != null)
            {
                if (ImGui.IsMouseReleased(0))
                {
                    //_linkUnderConstruction = null;
                    return;
                }
                var luc = _linkUnderConstruction;
                var node_inp = luc.OutputNodeIndex == -1 ? null : _nodes[luc.OutputNodeIndex];
                var node_out = luc.InputNodeIndex == -1 ? null : _nodes[luc.InputNodeIndex];

                var p1 = node_inp == null ? ImGui.GetMousePos() : GetScreenPosFrom(node_inp.GetOutputSlotPos(luc.OutputSlotIndex));
                var p2 = node_out == null ? ImGui.GetMousePos() : GetScreenPosFrom(node_out.GetInputSlotPos(luc.InputSlotIndex));
                _drawList.AddBezierCurve(p1, p1 + new Vector2(+50, 0), p2 + new Vector2(-50, 0), p2, TColors.ToUint(200, 200, 100, 255), 3.0f);
            }
        }

        public void MoveSelected(Vector2 delta)
        {
            foreach (var node in _nodes)
            {
                if (!node.IsSelected)
                    continue;

                node.Pos += delta;
            }
        }


        public Vector2 GetScreenPosFrom(Vector2 posOnCanvas)
        {
            return posOnCanvas * _scale + _scroll + _canvasPos;
        }

        public Vector2 GetChildPosFrom(Vector2 posOnCanvas)
        {
            return posOnCanvas * _scale + _scroll;
        }


        private void Init()
        {
            _nodes.Add(new Node()
            {
                ID = 0,
                Name = "MainTex",
                Pos = new Vector2(40, 50),
                Value = 0.5f,
                Color = TColors.White,
                InputsCount = 1,
                OutputsCount = 1,
            }
            );
            _nodes.Add(new Node()
            {
                ID = 1,
                Name = "MainTex2",
                Pos = new Vector2(140, 50),
                Value = 0.5f,
                Color = TColors.White,
                InputsCount = 1,
                OutputsCount = 1
            }
            );
            _nodes.Add(new Node()
            {
                ID = 2,
                Name = "MainTex3",
                Pos = new Vector2(240, 50),
                Value = 0.5f,
                Color = TColors.White,
                InputsCount = 1,
                OutputsCount = 1
            }
            );

            _links.Add(new NodeLink(0, 0, 2, 0));
            _links.Add(new NodeLink(1, 0, 2, 1));
            _initialized = true;
        }
    }
}


