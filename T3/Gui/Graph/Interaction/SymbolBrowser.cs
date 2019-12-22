using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.InputUi;
using T3.Gui.Styling;
using T3.Gui.TypeColors;

namespace T3.Gui.Graph.Interaction
{
    /// <summary>
    /// Represents the placeholder for a new <see cref="GraphNode"/> on the <see cref="GraphCanvas"/>. 
    /// It can be connected to other nodes and provide search functionality. It's basically the
    /// T2's CreateOperatorWindow.
    /// </summary>
    public class SymbolBrowser
    {
        private bool _keepNavEnableKeyboard;

        #region public API ------------------------------------------------------------------------
        public void OpenAt(Vector2 positionOnCanvas, Type filterInputType, Type filterOutputType)
        {
            _isOpen = true;
            PosOnCanvas = positionOnCanvas;
            _focusInputNextTime = true;
            _filter.FilterInputType = filterInputType;
            _filter.FilterOutputType = filterOutputType;
            _filter.SearchString = "";
            _selectedSymbolUi = null;
            _filter.UpdateIfNecessary();

            // Keep navigation setting to restore after window gets closed
            _keepNavEnableKeyboard = (ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.NavEnableKeyboard) != ImGuiConfigFlags.None;

            if (_selectedSymbolUi == null && _filter.MatchingSymbolUis.Count > 0)
            {
                _selectedSymbolUi = _filter.MatchingSymbolUis[0];
            }
        }

        public void Draw()
        {
            if (!_isOpen)
            {
                if (!ImGui.IsWindowFocused() || !ImGui.IsKeyReleased((int)Key.Tab))
                    return;
                
                Log.Debug("open create with tab");
                OpenAt(GraphCanvas.Current.InverseTransformPosition(ImGui.GetIO().MousePos), null, null);
                return;
            }

            // Disable keyboard navigation to allow cursor up down
            // This is being restored in Close()
            ImGui.GetIO().ConfigFlags &= ~ImGuiConfigFlags.NavEnableKeyboard;
            Current = this;

            _filter.UpdateIfNecessary();

            ImGui.PushID(UiId);
            {
                _posInWindow = GraphCanvas.Current.ChildPosFromCanvas(PosOnCanvas);
                _posInScreen = GraphCanvas.Current.TransformPosition(PosOnCanvas);
                _drawList = ImGui.GetWindowDrawList();

                DrawMatchesList();
                DrawSearchInput();
            }
            ImGui.PopID();
        }
        #endregion

        #region internal implementation -----------------------------------------------------------
        private void DrawSearchInput()
        {
            ImGui.SetCursorPos(_posInWindow);
            ImGui.SetNextItemWidth(90);

            if (_focusInputNextTime)
            {
                ImGui.SetKeyboardFocusHere();
                _focusInputNextTime = false;
            }

            ImGui.SetCursorPos(_posInWindow + new Vector2(1, 1));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(7, 6));
            ImGui.SetNextItemWidth(_size.X);
            ImGui.InputText("##filter", ref _filter.SearchString, 10);
            _drawList.AddRect(_posInScreen, _posInScreen + _size, Color.Gray);

            if (ImGui.IsKeyReleased((int)Key.CursorDown))
            {
                if (_filter.MatchingSymbolUis.Count > 0)
                {
                    var index = _filter.MatchingSymbolUis.IndexOf(_selectedSymbolUi);
                    index++;
                    index %= _filter.MatchingSymbolUis.Count;
                    _selectedSymbolUi = _filter.MatchingSymbolUis[index];
                }
            }
            else if (ImGui.IsKeyReleased((int)Key.CursorUp))
            {
                if (_filter.MatchingSymbolUis.Count > 0)
                {
                    var index = _filter.MatchingSymbolUis.IndexOf(_selectedSymbolUi);
                    index--;
                    if (index < 0)
                        index = _filter.MatchingSymbolUis.Count - 1;

                    _selectedSymbolUi = _filter.MatchingSymbolUis[index];
                }
            }
            else if (ImGui.IsKeyPressed((int)Key.Return))
            {
                if (_selectedSymbolUi != null)
                {
                    CreateInstance(_selectedSymbolUi.Symbol);
                }
            }

            var clickedOutside = ImGui.IsMouseClicked(0) && ImGui.IsWindowHovered();
            if (clickedOutside
                || ImGui.IsMouseClicked(1)
                || ImGui.GetIO().KeysDownDuration[(int)Key.Esc] > 0)
            {
                ConnectionMaker.Cancel();
                Log.Debug("Closing...");
                Close();
            }

            ImGui.PopStyleVar();

            if (ImGui.IsItemActive())
            {
                if (ImGui.IsMouseDragging(0))
                {
                    PosOnCanvas += GraphCanvas.Current.InverseTransformDirection(ImGui.GetIO().MouseDelta);
                }
            }
        }

        private void Close()
        {
            if (_keepNavEnableKeyboard)
                ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

            _isOpen = false;
        }

        private void DrawMatchesList()
        {
            ImGui.SetCursorPos(_posInWindow + new Vector2(_size.X + 1, 1));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5, 5));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(10, 10));

            if (ImGui.BeginChildFrame(999, ResultListSize))
            {
                if (_filter.MatchingSymbolUis.Count > 0 && !_filter.MatchingSymbolUis.Contains(_selectedSymbolUi))
                    _selectedSymbolUi = _filter.MatchingSymbolUis[0];
                
                if ((_selectedSymbolUi == null && SymbolUiRegistry.Entries.Values.Any()))
                    _selectedSymbolUi = SymbolUiRegistry.Entries.Values.FirstOrDefault();

                if (_filter.FilterInputType != null || _filter.FilterOutputType != null)
                {
                    ImGui.PushFont(Fonts.FontSmall);

                    var inputTypeName = _filter.FilterInputType != null
                                            ? TypeNameRegistry.Entries[_filter.FilterInputType]
                                            : string.Empty;

                    var outputTypeName = _filter.FilterOutputType != null
                                             ? TypeNameRegistry.Entries[_filter.FilterOutputType]
                                             : string.Empty;

                    var headerLabel = $"{inputTypeName} -> {outputTypeName}";
                    ImGui.TextDisabled(headerLabel);
                    ImGui.PopFont();
                }

                foreach (var symbolUi in _filter.MatchingSymbolUis)
                {
                    ImGui.PushID(symbolUi.Symbol.Id.GetHashCode());
                    {

                        var color = symbolUi.Symbol.OutputDefinitions.Count > 0
                                        ? TypeUiRegistry.GetPropertiesForType(symbolUi.Symbol.OutputDefinitions[0]?.ValueType).Color
                                        : Color.Gray;
                        ImGui.PushStyleColor(ImGuiCol.Header, ColorVariations.Operator.Apply(color).Rgba);
                        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ColorVariations.OperatorHover.Apply(color).Rgba);
                        ImGui.PushStyleColor(ImGuiCol.HeaderActive, ColorVariations.OperatorInputZone.Apply(color).Rgba);
                        ImGui.PushStyleColor(ImGuiCol.Text, ColorVariations.OperatorLabel.Apply(color).Rgba);
                        ImGui.Selectable("", symbolUi == _selectedSymbolUi);

                        if (ImGui.IsItemActivated())
                        {
                            CreateInstance(symbolUi.Symbol);
                        }

                        ImGui.SameLine();
                        ImGui.Text(symbolUi.Symbol.Name);
                        if (!String.IsNullOrEmpty(symbolUi.Symbol.Namespace))
                        {
                            ImGui.SameLine();
                            ImGui.TextDisabled(symbolUi.Symbol.Namespace);
                        }
                        ImGui.PopStyleColor(4);
                    }
                    ImGui.PopID();
                }

                var index = _filter.MatchingSymbolUis.IndexOf(_selectedSymbolUi);
                var rectMin = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos() + new Vector2(0, (index + 1) * ImGui.GetFrameHeight());
                var rectMax = rectMin + new Vector2(10, 10);
                var isVisible = ImGui.IsRectVisible(rectMin, rectMax);

                if (!isVisible)
                {
                    var keepPos = ImGui.GetCursorScreenPos();
                    ImGui.SetCursorScreenPos(rectMin);
                    ImGui.SetScrollHereY(0);
                    ImGui.SetCursorScreenPos(keepPos);
                }
            }

            ImGui.EndChildFrame();
            ImGui.PopStyleVar(2);
        }

        private void CreateInstance(Symbol symbol)
        {
            var parent = GraphCanvas.Current.CompositionOp.Symbol;
            var childUi= NodeOperations.CreateInstance(symbol, parent, PosOnCanvas);
            GraphCanvas.Current.SelectionHandler.SetElement(childUi);

            if (ConnectionMaker.TempConnection != null && symbol.InputDefinitions.Any())
            {
                var temp = ConnectionMaker.TempConnection;
                if (temp.SourceParentOrChildId == ConnectionMaker.UseDraftChildId)
                {
                    // connecting to output
                    ConnectionMaker.CompleteConnectionFromBuiltNode(parent, childUi.SymbolChild, _filter.GetOutputMatchingType(symbol, _filter.FilterInputType));
                }
                else
                {
                    // connecting to input
                    ConnectionMaker.CompleteConnectionIntoBuiltNode(parent, childUi.SymbolChild, _filter.GetInputMatchingType(symbol, _filter.FilterInputType));
                }
            }
            else
            {
                ConnectionMaker.Cancel();
            }

            Close();
        }


        private readonly SymbolFilter _filter = new SymbolFilter();
        #endregion



        //public List<Symbol.Connection> ConnectionsIn = new List<Symbol.Connection>();
        // public Symbol.Connection ConnectionOut = null;

        public Vector2 PosOnCanvas { get; private set; }

        public Vector2 OutputPositionOnScreen => _posInScreen + _size;

        private readonly Vector2 _size = SymbolChildUi.DefaultOpSize;

        private bool _focusInputNextTime;
        private Vector2 _posInScreen;
        private ImDrawListPtr _drawList;
        private Vector2 _posInWindow;

        private bool _isOpen;
        private static readonly Vector2 ResultListSize = new Vector2(300, 200);

        private SymbolUi _selectedSymbolUi;
        private static readonly int UiId = "DraftNode".GetHashCode();

        public static SymbolBrowser Current;
    }
}