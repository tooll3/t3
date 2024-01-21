using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Interaction.Variations;
using T3.Editor.Gui.Interaction.Variations.Model;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Interaction
{
    /// <summary>
    /// Represents a placeholder for a new <see cref="GraphNode"/> on the <see cref="GraphCanvas"/>. 
    /// It can be connected to other nodes and provide search functionality. It's basically the
    /// T2's CreateOperatorWindow.
    /// </summary>
    public class SymbolBrowser
    {
        #region public API ------------------------------------------------------------------------

        public void OpenAt(Vector2 positionOnCanvas, Type filterInputType, Type filterOutputType, bool onlyMultiInputs, string startingSearchString = "", System.Action<Symbol> overrideCreate = null)
        {
            // Scroll canvas to avoid symbol-browser close too edge
            var primaryGraphWindow = GraphWindow.GetPrimaryGraphWindow();
            if (primaryGraphWindow?.GraphCanvas == null)
            {
                Log.Warning("Can't open symbol browser without graph window.");
                return;
            }
            
            var canvas = primaryGraphWindow.GraphCanvas;
            if (canvas != null)
            {
                var screenPos = canvas.TransformPosition(positionOnCanvas);
                var screenRect = ImRect.RectWithSize(screenPos, SymbolChildUi.DefaultOpSize);
                screenRect.Expand(200 *canvas.Scale.X);
                var windowRect = ImRect.RectWithSize(ImGui.GetWindowPos(), ImGui.GetWindowSize());
                var tooCloseToEdge = !windowRect.Contains(screenRect);
                        
                var canvasPosition = canvas.InverseTransformPositionFloat(screenPos);
                if (tooCloseToEdge)
                {
                    var canvasRect = ImRect.RectWithSize(canvasPosition, SymbolChildUi.DefaultOpSize);
                    canvasRect.Expand(400);
                    canvas.FitAreaOnCanvas(canvasRect);
                }
            }


            //_prepareCommand = prepareCommand;
            _overrideCreate = overrideCreate;
            IsOpen = true;
            PosOnCanvas = positionOnCanvas;
            _focusInputNextTime = true;
            _filter.FilterInputType = filterInputType;
            _filter.FilterOutputType = filterOutputType;
            _filter.SearchString = startingSearchString;
            _selectedSymbolUi = null;
            _filter.OnlyMultiInputs = onlyMultiInputs;
            _filter.UpdateIfNecessary(forceUpdate: true);
            THelpers.DisableImGuiKeyboardNavigation();

            if (_selectedSymbolUi == null && _filter.MatchingSymbolUis.Count > 0)
            {
                _selectedSymbolUi = _filter.MatchingSymbolUis[0];
            }
        }

        public void Draw()
        {
            if (!IsOpen)
            {
                var hasFocus =  ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows);

                var anythingActive = ImGui.IsAnyItemActive();
                if (!hasFocus || anythingActive || !ImGui.IsKeyReleased((ImGuiKey)Key.Tab))
                    return;

                if (NodeSelection.GetSelectedChildUis().Count() != 1)
                {
                    ConnectionMaker.StartOperation("Add operator");
                    
                    var screenPos = ImGui.GetIO().MousePos + new Vector2(-4, -20);
                    var canvasPosition = GraphCanvas.Current.InverseTransformPositionFloat(screenPos);
                    
                    OpenAt(canvasPosition, null, null, false);
                    
                    return;
                }

                var childUi = NodeSelection.GetSelectedChildUis().ToList()[0];
                {
                    var instance = NodeSelection.GetInstanceForSymbolChildUi(childUi);
                    if (instance == null)
                    {
                        NodeSelection.Clear();
                        return;
                    }
                    
                    var screenPos = GraphCanvas.Current.TransformPosition(childUi.PosOnCanvas);
                    var screenRect = ImRect.RectWithSize(screenPos, SymbolChildUi.DefaultOpSize);
                    screenRect.Expand(200 *GraphCanvas.Current.Scale.X);
                    var windowRect = ImRect.RectWithSize(ImGui.GetWindowPos(), ImGui.GetWindowSize());
                    var tooCloseToEdge = !windowRect.Contains(screenRect);
                    
                    var canvasPosition = GraphCanvas.Current.InverseTransformPositionFloat(screenPos);
                    if (tooCloseToEdge)
                    {
                        var canvasRect = ImRect.RectWithSize(canvasPosition, SymbolChildUi.DefaultOpSize);
                        canvasRect.Expand(400);
                        GraphCanvas.Current.FitAreaOnCanvas(canvasRect);
                    }
                    
                    ConnectionMaker.OpenBrowserWithSingleSelection(this, childUi, instance);
                }
                return;
            }

            FrameStats.Current.OpenedPopUpName = "SymbolBrowser";

            _filter.UpdateIfNecessary();

            ImGui.PushID(_uiId);
            {
                var posInWindow = GraphCanvas.Current.ChildPosFromCanvas(PosOnCanvas);
                _posInScreen = GraphCanvas.Current.TransformPosition(PosOnCanvas);
                _drawList = ImGui.GetWindowDrawList();

                ImGui.SetNextWindowFocus();

                var browserPositionInWindow = posInWindow + BrowserPositionOffset;
                var browserSize = ResultListSize;

                ClampPanelToCanvas(ref browserPositionInWindow, ref browserSize);

                ImGui.SetCursorPos(browserPositionInWindow);

                DrawResultsList(browserSize);

                if (_selectedSymbolUi != null)
                {
                    if (_filter.PresetFilterString != null && (_filter.WasUpdated || _selectedItemChanged))
                    {
                        _matchingPresets.Clear();
                        var presetPool = VariationHandling.GetOrLoadVariations(_selectedSymbolUi.Symbol.Id);
                        if (presetPool != null)
                        {
                            _matchingPresets.AddRange(presetPool.Variations.FindAll(v => v.IsPreset && v.Title.Contains(_filter.PresetFilterString,
                                                                                             StringComparison.InvariantCultureIgnoreCase)));
                        }
                    }

                    if (_filter.PresetFilterString != null)
                    {
                        UiListHelpers.AdvanceSelectedItem(_matchingPresets, ref _selectedPreset, 0);
                        DrawPresetPanel(browserPositionInWindow, new Vector2(140, browserSize.Y));
                    }
                    else
                    {
                        _selectedPreset = null;
                        DrawDescriptionPanel(browserPositionInWindow, browserSize);
                    }
                }

                DrawSearchInput(posInWindow, _posInScreen, _size * T3Ui.UiScaleFactor);
            }

            ImGui.PopID();
        }
        #endregion

        private System.Action<Symbol> _overrideCreate = null;

        //private bool IsSearchingPresets => _filter.MatchingPresets.Count > 0;

        private void DrawSearchInput(Vector2 posInWindow, Vector2 posInScreen, Vector2 size)
        {
            if (_focusInputNextTime)
            {
                ImGui.SetKeyboardFocusHere();
                _focusInputNextTime = false;
                _selectedItemChanged = true;
            }

            ImGui.SetCursorPos(posInWindow);

            var padding = new Vector2(7, 6);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, padding);
            ImGui.SetNextItemWidth(size.X);

            ImGui.InputText("##symbolbrowserfilter", ref _filter.SearchString, 10);

            // Search input outline
            _drawList.AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), UiColors.Gray);

            if (ImGui.IsKeyPressed((ImGuiKey)Key.Return))
            {
                if (_selectedSymbolUi != null)
                {
                    CreateInstance(_selectedSymbolUi.Symbol);
                }
            }

            if (_filter.WasUpdated)
            {
                _selectedSymbolUi = _filter.MatchingSymbolUis.Count > 0
                                        ? _filter.MatchingSymbolUis[0]
                                        : null;
                _selectedItemChanged = true;
            }

            var clickedOutside = ImGui.IsMouseClicked(ImGuiMouseButton.Left) && ImGui.IsWindowHovered();
            var shouldCancelConnectionMaker = clickedOutside
                                              || ImGui.IsMouseClicked(ImGuiMouseButton.Right)
                                              || ImGui.IsKeyDown((ImGuiKey)Key.Esc);

            if (shouldCancelConnectionMaker)
            {
                //ConnectionMaker.AbortOperation();
                Cancel();
            }

            ImGui.PopStyleVar();

            if (!ImGui.IsItemActive())
                return;

            if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                PosOnCanvas += GraphCanvas.Current.InverseTransformDirection(ImGui.GetIO().MouseDelta);
            }
        }

        private void Cancel()
        {
            ConnectionMaker.AbortOperation();
            // if (_prepareCommand != null)
            // {
            //     _prepareCommand.Undo();
            //     _prepareCommand = null;
            // }

            Close();
        }

        private void Close()
        {
            THelpers.RestoreImGuiKeyboardNavigation();
            IsOpen = false;
            var win = GraphWindow.GetPrimaryGraphWindow();
            win?.Focus();
        }

        private void DrawResultsList(Vector2 size)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5, 5));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(10, 10));
            
            ImGui.PushStyleColor(ImGuiCol.FrameBg, UiColors.BackgroundPopup.Rgba);
            //var itemForHelpIsHovered = false;

            if (ImGui.BeginChildFrame(999, size))
            {
                if (_filter.PresetFilterString == null)
                {
                    if (ImGui.IsKeyReleased((ImGuiKey)Key.CursorDown))
                    {
                        UiListHelpers.AdvanceSelectedItem(_filter.MatchingSymbolUis, ref _selectedSymbolUi, 1);
                        _selectedItemChanged = true;
                    }
                    else if (ImGui.IsKeyReleased((ImGuiKey)Key.CursorUp))
                    {
                        UiListHelpers.AdvanceSelectedItem(_filter.MatchingSymbolUis, ref _selectedSymbolUi, -1);
                        _selectedItemChanged = true;
                    }
                }

                var gotAMatch = _filter.MatchingSymbolUis.Count > 0 && !_filter.MatchingSymbolUis.Contains(_selectedSymbolUi);
                if (gotAMatch)
                    _selectedSymbolUi = _filter.MatchingSymbolUis[0];

                if ((_selectedSymbolUi == null && SymbolUiRegistry.Entries.Values.Any()))
                    _selectedSymbolUi = SymbolUiRegistry.Entries.Values.FirstOrDefault();

                PrintTypeFilter();

                var configUserName = "user." + UserSettings.Config.UserName + ".";
                var compositionNameSpace = "";
                var currentMainComposition = GraphWindow.GetMainComposition();
                if (currentMainComposition != null)
                {
                    compositionNameSpace = currentMainComposition.Symbol.Namespace;
                }

                foreach (var symbolUi in _filter.MatchingSymbolUis)
                {
                    var symbolHash = symbolUi.Symbol.Id.GetHashCode();
                    ImGui.PushID(symbolHash);
                    {
                        var symbolNamespace = symbolUi.Symbol.Namespace;
                        var isRelevantNamespace = symbolNamespace.StartsWith("lib.")
                                                  || symbolNamespace.StartsWith("examples.lib.")
                                                  || symbolNamespace.StartsWith(configUserName)
                                                  || symbolNamespace.StartsWith(compositionNameSpace);

                        var color = symbolUi.Symbol.OutputDefinitions.Count > 0
                                        ? TypeUiRegistry.GetPropertiesForType(symbolUi.Symbol.OutputDefinitions[0]?.ValueType).Color
                                        : UiColors.Gray;

                        if (!isRelevantNamespace)
                        {
                            color = color.Fade(0.4f);
                        }

                        ImGui.PushStyleColor(ImGuiCol.Header, ColorVariations.OperatorBackground.Apply(color).Rgba);

                        var hoverColor = ColorVariations.OperatorBackgroundHover.Apply(color).Rgba;
                        hoverColor.W = 0.1f;
                        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, hoverColor);
                        //ImGui.PushStyleColor(ImGuiCol.HeaderActive, ColorVariations.OperatorInputZone.Apply(color).Rgba);
                        ImGui.PushStyleColor(ImGuiCol.Text, ColorVariations.OperatorLabel.Apply(color).Rgba);

                        var isSelected = symbolUi == _selectedSymbolUi;

                        _selectedItemChanged |= ImGui.Selectable($"##Selectable{symbolHash.ToString()}", isSelected);
                        //bool selectionChangedToThis = isSelected && _selectedItemChanged;

                        var isHovered = ImGui.IsItemHovered();
                        var hasMouseMoved = ImGui.GetIO().MouseDelta.LengthSquared() > 0;
                        if (hasMouseMoved && isHovered)
                        {
                            _selectedSymbolUi = symbolUi;
                            //_timeDescriptionSymbolUiLastHovered = DateTime.Now;
                            _selectedItemChanged = true;
                        }
                        else if (_selectedItemChanged && _selectedSymbolUi == symbolUi)
                        {
                            UiListHelpers.ScrollToMakeItemVisible();
                            _selectedItemChanged = false;
                        }

                        if (ImGui.IsItemActivated())
                        {
                            CreateInstance(symbolUi.Symbol);
                        }

                        ImGui.SameLine();

                        ImGui.TextUnformatted(symbolUi.Symbol.Name);
                        ImGui.SameLine();

                        if (!string.IsNullOrEmpty(symbolNamespace))
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, UiColors.Text.Fade(0.5f).Rgba);
                            ImGui.Text(symbolNamespace);
                            ImGui.PopStyleColor();
                            ImGui.SameLine();
                        }

                        ImGui.NewLine();

                        ImGui.PopStyleColor(3);
                    }
                    ImGui.PopID();
                }
            }

            ImGui.EndChildFrame();

            ImGui.PopStyleColor();
            ImGui.PopStyleVar(2);
        }

        private void PrintTypeFilter()
        {
            if (_filter.FilterInputType == null && _filter.FilterOutputType == null)
                return;

            ImGui.PushFont(Fonts.FontSmall);

            var inputTypeName = _filter.FilterInputType != null
                                    ? TypeNameRegistry.Entries[_filter.FilterInputType]
                                    : string.Empty;

            var outputTypeName = _filter.FilterOutputType != null
                                     ? TypeNameRegistry.Entries[_filter.FilterOutputType]
                                     : string.Empty;

            var isMultiInput = _filter.OnlyMultiInputs ? "[..]" : "";

            var headerLabel = $"{inputTypeName}{isMultiInput}  -> {outputTypeName}";
            ImGui.TextDisabled(headerLabel);
            ImGui.PopFont();
        }

        private static void ClampPanelToCanvas(ref Vector2 position, ref Vector2 size)
        {
            var maxXPos = position.X + size.X;
            var maxYPos = position.Y + size.Y;

            var windowSize = GraphCanvas.Current.WindowSize;

            var shouldShiftLeft = maxXPos > windowSize.X;
            var xPositionOffset = shouldShiftLeft ? windowSize.X - maxXPos : 0;

            var yOverflow = maxYPos > windowSize.Y;
            var yShrinkage = yOverflow ? windowSize.Y - maxYPos : 0;

            position.X += xPositionOffset;
            size.Y += yShrinkage;
        }

        private Variation _selectedPreset;

        private void DrawPresetPanel(Vector2 position, Vector2 size)
        {
            //size.X = 120;
            if (!TryFindValidPanelPosition(ref position, size))
                return;

            ImGui.PushStyleColor(ImGuiCol.FrameBg, UiColors.BackgroundPopup.Rgba);
            ImGui.SetCursorPos(position);
            if (ImGui.BeginChildFrame(998, size))
            {
                if (ImGui.IsKeyReleased((ImGuiKey)Key.CursorDown))
                {
                    UiListHelpers.AdvanceSelectedItem(_matchingPresets, ref _selectedPreset, 1);
                }
                else if (ImGui.IsKeyReleased((ImGuiKey)Key.CursorUp))
                {
                    UiListHelpers.AdvanceSelectedItem(_matchingPresets, ref _selectedPreset, -1);
                }

                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.One); // Padding between panels

                ImGui.PushFont(Fonts.FontLarge);
                ImGui.TextUnformatted(_selectedSymbolUi.Symbol.Name);
                ImGui.PopFont();
                ImGui.TextUnformatted("Presets");
                ImGui.Dummy(Vector2.One * 10);

                if (_matchingPresets.Count == 0)
                {
                    CustomComponents.EmptyWindowMessage("No presets found");
                }
                else
                {
                    foreach (var p in _matchingPresets)
                    {
                        if (ImGui.Selectable(p.Title, _selectedPreset == p))
                        {
                            _selectedPreset = p;
                            CreateInstance(_selectedSymbolUi.Symbol);
                        }
                    }
                }

                ImGui.PopStyleVar();
                ImGui.EndChildFrame();
            }
            ImGui.PopStyleColor();
            
        }

        private void DrawDescriptionPanel(Vector2 position, Vector2 size)
        {
            if (_selectedSymbolUi == null)
                return;

            var hasExamples = ExampleSymbolLinking.ExampleSymbols.TryGetValue(_selectedSymbolUi.Symbol.Id, out var examples2)
                              && examples2.Count > 0;

            var hasDescription = !string.IsNullOrEmpty(_selectedSymbolUi.Description);

            if (!hasExamples && !hasDescription)
                return;

            if (!TryFindValidPanelPosition(ref position, size))
                return;

            ImGui.SetCursorPos(position);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.One); // Padding between panels
            ImGui.PushStyleColor(ImGuiCol.FrameBg, UiColors.BackgroundPopup.Rgba);

            if (ImGui.BeginChildFrame(998, size))
            {
                if (!string.IsNullOrEmpty(_selectedSymbolUi.Description))
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
                    ImGui.TextWrapped(_selectedSymbolUi.Description);
                    ImGui.PopStyleColor();
                }

                if (hasExamples)
                {
                    ImGui.Dummy(new Vector2(10, 10));
                    ListExampleOperators(_selectedSymbolUi);
                }

                ImGui.EndChildFrame();
            }

            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
        }

        private static bool TryFindValidPanelPosition(ref Vector2 position, Vector2 size)
        {
            var maxXPos = position.X + size.X + ResultListSize.X;
            var wouldExceedWindowBounds = maxXPos >= GraphCanvas.Current.WindowSize.X;

            if (wouldExceedWindowBounds)
            {
                position.X -= size.X;

                //if it simply doesn't fit on screen, return
                if (position.X <= 0)
                    return false;
            }
            else
            {
                position.X += size.X;
            }

            return true;
        }

        public static void ListExampleOperators(SymbolUi itemForHelp)
        {
            if (!ExampleSymbolLinking.ExampleSymbols.TryGetValue(itemForHelp.Symbol.Id, out var examples))
                return;

            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f * ImGui.GetStyle().Alpha);
            foreach (var guid in examples)
            {
                const string label = "Example";
                DrawExampleOperator(guid, label);
            }

            ImGui.PopStyleVar();
        }

        public static void DrawExampleOperator(Guid exampleId, string label)
        {
            var symbolUi = SymbolUiRegistry.Entries[exampleId];
            var color = symbolUi.Symbol.OutputDefinitions.Count > 0
                            ? TypeUiRegistry.GetPropertiesForType(symbolUi.Symbol.OutputDefinitions[0]?.ValueType).Color
                            : UiColors.Gray;

            ImGui.PushStyleColor(ImGuiCol.Button, ColorVariations.OperatorBackground.Apply(color).Rgba);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColorVariations.OperatorBackgroundHover.Apply(color).Rgba);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ColorVariations.OperatorBackgroundHover.Apply(color).Rgba);
            ImGui.PushStyleColor(ImGuiCol.Text, ColorVariations.OperatorLabel.Apply(color).Rgba);

            ImGui.SameLine();

            var restSpace = ImGui.GetWindowWidth() - ImGui.GetCursorPos().X;
            if (restSpace < 100)
            {
                ImGui.Dummy(new Vector2(10,10));
            }

            ImGui.Button(label);
            SymbolTreeMenu.HandleDragAndDropForSymbolItem(symbolUi.Symbol);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
            }
            
            if (!string.IsNullOrEmpty(symbolUi.Description))
            {
                CustomComponents.TooltipForLastItem(symbolUi.Description);
            }

            ImGui.PopStyleColor(4);
        }

        private void CreateInstance(Symbol symbol)
        {
            if(_overrideCreate != null)
            {
                Close();
                _overrideCreate(symbol);
                return;
            }

            var commandsForUndo = new List<ICommand>();
            var parent = GraphCanvas.Current.CompositionOp.Symbol;

            var addSymbolChildCommand = new AddSymbolChildCommand(parent, symbol.Id) { PosOnCanvas = PosOnCanvas };
            commandsForUndo.Add(addSymbolChildCommand);
            addSymbolChildCommand.Do();
            var newSymbolChild = parent.Children.Single(entry => entry.Id == addSymbolChildCommand.AddedChildId);

            // Select new node
            var symbolUi = SymbolUiRegistry.Entries[parent.Id];
            var newChildUi = symbolUi.ChildUis.Find(s => s.Id == newSymbolChild.Id);
            if (newChildUi == null)
            {
                Log.Warning("Unable to create new operator");
                return;
            }
            var newInstance = GraphCanvas.Current.CompositionOp.Children.Single(child => child.SymbolChildId == newChildUi.Id);

            var presetPool = VariationHandling.GetOrLoadVariations(_selectedSymbolUi.Symbol.Id);
            if (presetPool != null && _selectedPreset != null)
            {
                presetPool.Apply(newInstance, _selectedPreset);
            }

            NodeSelection.SetSelectionToChildUi(newChildUi, newInstance);

            // if (_prepareCommand != null)
            // {
            //     additionalCommands.Add(_prepareCommand);
            // }
            

            foreach (var c in ConnectionMaker.TempConnections)
            {
                switch (c.GetStatus())
                {
                    case ConnectionMaker.TempConnection.Status.SourceIsDraftNode:
                        var outputDef = newSymbolChild.Symbol.GetOutputMatchingType(c.ConnectionType);
                        if (outputDef == null)
                        {
                            Log.Error("Failed to find matching output connection type " + c.ConnectionType);
                            return;
                        }

                        var newConnectionToSource = new Symbol.Connection(sourceParentOrChildId: newSymbolChild.Id,
                                                                          sourceSlotId: outputDef.Id,
                                                                          targetParentOrChildId: c.TargetParentOrChildId,
                                                                          targetSlotId: c.TargetSlotId);
                        var addConnectionCommand = new AddConnectionCommand(parent, newConnectionToSource, c.MultiInputIndex);
                        addConnectionCommand.Do();
                        commandsForUndo.Add(addConnectionCommand);
                        break;

                    case ConnectionMaker.TempConnection.Status.TargetIsDraftNode:
                        var inputDef = newSymbolChild.Symbol.GetInputMatchingType(c.ConnectionType);
                        if (inputDef == null)
                        {
                            Log.Warning("Failed to complete node creation");
                            return;
                        }

                        var newConnectionToInput = new Symbol.Connection(sourceParentOrChildId: c.SourceParentOrChildId,
                                                                         sourceSlotId: c.SourceSlotId,
                                                                         targetParentOrChildId: newSymbolChild.Id,
                                                                         targetSlotId: inputDef.Id);
                        var connectionCommand = new AddConnectionCommand(parent, newConnectionToInput, c.MultiInputIndex);
                        connectionCommand.Do();
                        commandsForUndo.Add(connectionCommand);
                        break;
                }
            }

            // var newCommand = new MacroCommand("Insert Op", commands);
            // UndoRedoStack.Add(newCommand);
            ConnectionMaker.CompleteOperation(commandsForUndo, "Insert Op " + newChildUi.SymbolChild.ReadableName);
            ParameterPopUp.NodeIdRequestedForParameterWindowActivation = newSymbolChild.Id;
            Close();
        }

        private readonly SymbolFilter _filter = new();

        public Vector2 PosOnCanvas { get; private set; }
        public Vector2 OutputPositionOnScreen => _posInScreen + _size;
        public bool IsOpen;

        private readonly Vector2 _size = SymbolChildUi.DefaultOpSize;
        private static Vector2 BrowserPositionOffset => new(0, 40);

        private bool _focusInputNextTime;
        private Vector2 _posInScreen;
        private ImDrawListPtr _drawList;
        private bool _selectedItemChanged;

        private static Vector2 ResultListSize => new Vector2(250, 300) * T3Ui.UiScaleFactor;
        private readonly Vector4 _namespaceColor = new Color(1, 1, 1, 0.4f);

        private SymbolUi _selectedSymbolUi;
        private static readonly int _uiId = "DraftNode".GetHashCode();
        private readonly List<Variation> _matchingPresets = new();
    }
}