using ImGuiNET;
using imHelpers;
using System;
using System.Collections.Generic;
using System.Numerics;
using t3.iuhelpers;
using T3.Core.Operator;
using T3.Gui.Graph;
using T3.Gui.Selection;

namespace t3.graph
{
    /// <summary>
    /// A mock implementation of a future graph renderer
    /// </summary>
    public class GraphCanvasWindow
    {
        public GraphCanvasWindow(Instance opInstance, string windowTitle = "Graph windows")
        {
            _compositionOp = opInstance;
            _windowTitle = windowTitle;
            _selectionFence = new SelectionFence(this);
        }

        public SelectionHandler SelectionHandler { get; set; } = new SelectionHandler();
        SelectionFence _selectionFence;

        /// <summary>
        /// Renders a canvas window
        /// </summary>
        /// <returns>false if closed</returns>
        public bool Draw()
        {
            bool opened = true;
            //var compOpUi = InstanceUiRegistry.Instance.UiEntries[_compositionOp.Id];
            var uniqueTitle = _windowTitle + "##" + _windowGui;

            if (ImGui.Begin(uniqueTitle, ref opened))
            {
                _uiChildren = InstanceUiRegistry.Instance.UiEntries[_compositionOp.Symbol.Id];
                _drawList = ImGui.GetWindowDrawList();
                _overlayDrawList = ImGui.GetOverlayDrawList();

                DrawNodeList();
                ImGui.SameLine();
                DrawCanvas();
            }
            ImGui.End();
            return opened;
        }



        private void DrawNodeList()
        {
            // Draw a list of nodes on the left side
            ImGui.BeginChild("node_list", new Vector2(80, 0));
            {
                ImGui.Text("Nodes");
                ImGui.Separator();


                //var allUiEntriesForChildrenOfSymbol = InstanceUiRegistry.Instance.UiEntries[symbol.Id];
                //var uiEntryForASpecificInstance = allUiEntriesForChildrenOfSymbol[instance.Id];

                foreach (var pair in _uiChildren)
                {
                    var instanceUi = pair.Value;
                    ImGui.PushID(pair.Key.GetHashCode());
                    {
                        var name = instanceUi.ReadableName;
                        if (ImGui.Selectable(name, instanceUi.IsSelected))
                        {
                            // Change selection 
                            foreach (var nodePair in _uiChildren)
                            {
                                var node2 = nodePair.Value;
                                if (node2.IsSelected && node2 != instanceUi)
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


        //public void StartLinkFromInput(Node nodeWithInput, int inputSlotIndex)
        //{
        //    _linkUnderConstruction = new NodeLink()
        //    {
        //        OutputNodeIndex = -1,
        //        OutputSlotIndex = -1,
        //        InputSlotIndex = inputSlotIndex,
        //        InputNodeIndex = nodeWithInput.ID,
        //    };
        //}


        //public void StartLinkFromOutput(Node nodeWithOutput, int outputSlotIndex)
        //{
        //    _linkUnderConstruction = new NodeLink()
        //    {
        //        OutputSlotIndex = outputSlotIndex,
        //        OutputNodeIndex = nodeWithOutput.ID,
        //        InputNodeIndex = -1,
        //        InputSlotIndex = -1,
        //    };
        //}

        //public void CompleteLinkToOutput(Node nodeWithOutput, int outputSlotIndex)
        //{
        //    //if (_linkUnderConstruction == null)
        //    //    return;

        //    //_linkUnderConstruction.OutputSlotIndex = outputSlotIndex;
        //    //_linkUnderConstruction.OutputNodeIndex = nodeWithOutput.ID;
        //    //_links.Add(_linkUnderConstruction);
        //    //_linkUnderConstruction = null;
        //}

        //public void CompleteLinkToInput(Node nodeWithInput, int inputSlotIndex)
        //{
        //    //if (_linkUnderConstruction == null)
        //    //    return;

        //    //_linkUnderConstruction.InputSlotIndex = inputSlotIndex;
        //    //_linkUnderConstruction.InputNodeIndex = nodeWithInput.ID;
        //    //_links.Add(_linkUnderConstruction);
        //    //_linkUnderConstruction = null;
        //}


        //public void CancelLink()
        //{
        //    _linkUnderConstruction = null;
        //}


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

                // Damp scaling
                _scale = Im.Lerp(_scale, _scaleTarget, _io.DeltaTime * 20);
                _scroll = Im.Lerp(_scroll, _scrollTarget, _io.DeltaTime * 20);

                THelpers.DebugWindowRect("window");
                ImGui.BeginChild("scrolling_region", new Vector2(0, 0), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
                {
                    THelpers.DebugWindowRect("window.scrollingRegion");
                    _canvasWindowPos = ImGui.GetWindowPos();
                    _size = ImGui.GetWindowSize();
                    _drawList.PushClipRect(_canvasWindowPos, _canvasWindowPos + _size);

                    // Canvas interaction --------------
                    if (ImGui.IsWindowHovered())
                    {
                        if (ImGui.IsMouseDragging(1))
                        {
                            _scrollTarget += _io.MouseDelta;
                        }

                        if (ImGui.IsMouseDoubleClicked(0))
                        {
                            QuickCreateWindow.OpenAtPosition(ImGui.GetMousePos(), _compositionOp.Symbol, CanvasPosFromScreen(ImGui.GetMousePos()));
                        }

                        // Zoom with mouse wheel
                        if (_io.MouseWheel != 0)
                        {
                            const float zoomSpeed = 1.2f;
                            var focusCenter = (_mouse - _scroll - _canvasWindowPos) / _scale;

                            _overlayDrawList.AddCircle(focusCenter + ImGui.GetWindowPos(), 10, Color.Red.ToUint());

                            if (_io.MouseWheel < 0.0f)
                            {
                                for (float zoom = _io.MouseWheel; zoom < 0.0f; zoom += 1.0f)
                                {
                                    _scaleTarget = Im.Max(0.3f, _scaleTarget / zoomSpeed);
                                }
                            }

                            if (_io.MouseWheel > 0.0f)
                            {
                                for (float zoom = _io.MouseWheel; zoom > 0.0f; zoom -= 1.0f)
                                {
                                    _scaleTarget = Im.Min(3.0f, _scaleTarget * zoomSpeed);
                                }
                            }

                            Vector2 shift = _scrollTarget + (focusCenter * _scaleTarget);
                            _scrollTarget += _mouse - shift - _canvasWindowPos;
                        }

                        ImGui.SetScrollY(0);    // HACK: prevent jump of scroll position by accidental scrolling
                    }

                    // Draw Grid ------------
                    {
                        var gridSize = 64.0f * _scale;
                        for (float x = _scroll.X % gridSize; x < _size.X; x += gridSize)
                        {
                            _drawList.AddLine(
                                new Vector2(x, 0.0f) + _canvasWindowPos,
                                new Vector2(x, _size.Y) + _canvasWindowPos,
                                new Color(0.5f, 0.5f, 0.5f, 0.1f).ToUint());
                        }

                        for (float y = _scroll.Y % gridSize; y < _size.Y; y += gridSize)
                        {
                            _drawList.AddLine(
                                new Vector2(0.0f, y) + _canvasWindowPos,
                                new Vector2(_size.X, y) + _canvasWindowPos,
                                new Color(0.5f, 0.5f, 0.5f, 0.1f).ToUint());
                        }
                    }

                    // Draw links
                    DrawLinks();

                    // Draw nodes
                    foreach (var instanceUi in InstanceUiRegistry.Instance.UiEntries[_compositionOp.Symbol.Id].Values)
                    {
                        GraphOperator.DrawOnCanvas(instanceUi, this);
                    }

                    _debugMessages += ImGui.IsAnyItemHovered() ? "anyItemHovered " : "";
                    _debugMessages += ImGui.IsWindowHovered() ? "isWindowHovered " : "";
                    _debugMessages += ImGui.IsItemHovered() ? "isWindowHovered " : "";



                    _selectionFence.Draw();

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
        public Vector2 CanvasPosFromScreen(Vector2 screenPos)
        {
            return (screenPos - _scroll - _canvasWindowPos) / _scale;
        }


        /// <summary>
        /// Get screen position applying canas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public Vector2 ScreenPosFromCanvas(Vector2 posOnCanvas)
        {
            return posOnCanvas * _scale + _scroll + _canvasWindowPos;
        }

        public ImRect CanvasRectFromScreen(ImRect screenRect)
        {
            return new ImRect(CanvasPosFromScreen(screenRect.Min), CanvasPosFromScreen(screenRect.Max));
        }


        /// <summary>
        /// Get relative position within canvas by applying zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public Vector2 ChildPosFromCanvas(Vector2 posOnCanvas)
        {
            return posOnCanvas * _scale + _scroll;
        }



        private Guid _windowGui = Guid.NewGuid();
        private string _windowTitle;
        private Instance _compositionOp;

        public Dictionary<Guid, SymbolChildUi> UiChildrenById => _uiChildren;
        private Dictionary<Guid, SymbolChildUi> _uiChildren;


        //        private NodeLink _linkUnderConstruction;



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

        /// <summary>
        /// The position of the canvas window-panel within Application window
        /// </summary>
        public Vector2 _canvasWindowPos;
        public float _scale = 1; //the damped scale factor {read only}
        float _scaleTarget = 1;

        string _debugMessages = "";
        ImGuiIOPtr _io;
    }
}