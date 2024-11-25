using System.Diagnostics;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Interaction.Variations;
using T3.Editor.Gui.Interaction.Variations.Model;
using T3.Editor.Gui.MagGraph.Model;
using T3.Editor.Gui.MagGraph.Ui;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.UiModel;
using T3.SystemUi;

namespace T3.Editor.Gui.MagGraph.Interaction;

/// <summary>
/// Controls when and how a placeholder is shown and used to create new items / outputs etc.
/// </summary>
internal sealed class PlaceholderCreation
{
    internal MagGraphItem? PlaceholderItem;

    private sealed class PlaceholderSelectable : ISelectableCanvasObject
    {
        public Guid Id { get; }
        public Vector2 PosOnCanvas { get; set; }
        public Vector2 Size { get; set; }
    }

    private static readonly PlaceholderSelectable _placeHoldSelectable = new();

    public static Guid PlaceHolderId = Guid.Parse("ffffffff-eeee-47C7-A17F-E297672EE1F3");

    public void OpenOnCanvas(GraphUiContext context, Vector2 posOnCanvas)
    {
        context.MacroCommand = new MacroCommand("Insert Operator");

        PlaceholderItem = new MagGraphItem
                              {
                                  Selectable = _placeHoldSelectable,
                                  PosOnCanvas = posOnCanvas,
                                  Id = PlaceHolderId,
                                  Size = MagGraphItem.GridSize,
                                  Variant = MagGraphItem.Variants.Placeholder,
                              };

        context.Layout.Items[PlaceHolderId] = PlaceholderItem;
        _focusInputNextTime = true;

        //_filter.PresetFilterString = string.Empty;
        _filter.WasUpdated = true;
        _filter.SearchString = string.Empty;
        _selectedSymbolUi = null;
        _filter.UpdateIfNecessary(context.Selector, forceUpdate: true);
        
        _selectedItemChanged = true;
    }

    public void Cancel(GraphUiContext context)
    {
        context.MacroCommand?.Undo();
        Reset(context);
    }

    private void Close(GraphUiContext context)
    {
        if (context.MacroCommand != null)
            UndoRedoStack.Add(context.MacroCommand);

        Reset(context);
    }

    private void Reset(GraphUiContext context)
    {
        context.MacroCommand = null;

        if (PlaceholderItem == null)
            return;

        context.Layout.Items.Remove(PlaceHolderId);
        PlaceholderItem = null;

        THelpers.RestoreImGuiKeyboardNavigation();
    }

    internal void DrawPlaceholder(GraphUiContext context, ImDrawListPtr drawList)
    {
        if (PlaceholderItem == null)
            return;

        FrameStats.Current.OpenedPopUpName = "SymbolBrowser";

        _filter.UpdateIfNecessary(context.Selector);

        ImGui.PushID(_uiId);
        {
            // var posInWindow = pMin - ImGui.GetWindowPos();

            //ImGui.SetCursorPos(posInWindow);
            DrawSearchInput(context, drawList);

            // Might have been closed from input
            if (PlaceholderItem != null)
            {
                var pMin = context.Canvas.TransformPosition(PlaceholderItem.PosOnCanvas);
                var pMax = context.Canvas.TransformPosition(PlaceholderItem.Area.Max);
                DrawResultsList(new ImRect(pMin,pMax),  context);

                if (_selectedSymbolUi != null)
                {
                    if (_filter.PresetFilterString != string.Empty && (_filter.WasUpdated || _selectedItemChanged))
                    {
                        // TODO: Implement preset search
                    }
                }

                if (_focusInputNextTime)
                {
                    ImGui.SetKeyboardFocusHere();
                    _focusInputNextTime = false;
                    _selectedItemChanged = true;
                }
                
            }

        }

        ImGui.PopID();
    }

    private void DrawSearchInput(GraphUiContext context, ImDrawListPtr drawList)
    {
        Debug.Assert(PlaceholderItem != null);

        var canvasScale = context.Canvas.Scale.X;
        var item = this.PlaceholderItem;
        var pMin = context.Canvas.TransformPosition(item.PosOnCanvas);
        var pMax = context.Canvas.TransformPosition(item.PosOnCanvas + item.Size);
        var pMinVisible = pMin;
        var pMaxVisible = pMax;

        // Background and Outline
        drawList.AddRectFilled(pMinVisible + Vector2.One * canvasScale, pMaxVisible - Vector2.One,
                               UiColors.BackgroundFull, 6 * canvasScale);

        drawList.AddRect(pMinVisible, pMaxVisible, UiColors.ForegroundFull, 6 * canvasScale);

        if (_focusInputNextTime)
        {
            ImGui.SetKeyboardFocusHere();
            _focusInputNextTime = false;
            _selectedItemChanged = true;
        }

        var posInWindow = pMin - ImGui.GetWindowPos();
        ImGui.SetCursorPos(posInWindow);

        var padding = new Vector2(7, 6);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, padding);
        ImGui.PushStyleColor(ImGuiCol.FrameBg, Color.Transparent.Rgba);
        ImGui.SetNextItemWidth(item.Size.X);

        ImGui.InputText("##symbolBrowserFilter",
                        ref _filter.SearchString,
                        20, ImGuiInputTextFlags.AutoSelectAll);
        ImGui.PopStyleColor();

        if (ImGui.IsKeyPressed((ImGuiKey)Key.Return))
        {
            if (_selectedSymbolUi != null)
            {
                CreateInstance(context, _selectedSymbolUi.Symbol);
            }
        }

        if (_filter.WasUpdated)
        {
            _selectedSymbolUi = _filter.MatchingSymbolUis.Count > 0
                                    ? _filter.MatchingSymbolUis[0]
                                    : null;
            _selectedItemChanged = true;
        }

        var clickedOutside = false; //ImGui.IsMouseClicked(ImGuiMouseButton.Left) && ImGui.IsWindowHovered();
        var shouldCancelConnectionMaker = clickedOutside
                                          //|| ImGui.IsMouseClicked(ImGuiMouseButton.Right)
                                          || ImGui.IsKeyDown((ImGuiKey)Key.Esc);

        if (shouldCancelConnectionMaker)
        {
            //ConnectionMaker.AbortOperation();
            Cancel(context);
        }

        ImGui.PopStyleVar();

        if (!ImGui.IsItemActive())
            return;

        if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            PlaceholderItem.PosOnCanvas += context.Canvas.InverseTransformDirection(ImGui.GetIO().MouseDelta);
        }

        // Label...
        // ImGui.PushFont(Fonts.FontBold);
        // var labelSize = ImGui.CalcTextSize(item.ReadableName);
        // ImGui.PopFont();
        // var downScale = MathF.Min(1, MagGraphItem.Width * 0.9f / labelSize.X);
        // var labelPos = pMin + new Vector2(8, 7) * CanvasScale + new Vector2(0, -1);
        // labelPos = new Vector2(MathF.Round(labelPos.X), MathF.Round(labelPos.Y));
        // drawList.AddText(Fonts.FontBold,
        //                  Fonts.FontBold.FontSize * downScale * CanvasScale,
        //                  labelPos,
        //                  UiColors.ForegroundFull,
        //                  "Search...");
    }

    private void DrawResultsList(ImRect screenItemArea, GraphUiContext context)
    {
        var size = new Vector2(150, 235) * T3Ui.UiScaleFactor;
        var windowSize = ImGui.GetWindowSize();
        var windowPos = ImGui.GetWindowPos();
        Vector2 resultPosOnScreen= new Vector2(screenItemArea.Min.X, screenItemArea.Max.Y + 3);
        if (Orientation == MagGraphItem.Directions.Horizontal)
        {
            
            var y =  screenItemArea.GetCenter().Y + (-0.3f) * size.Y;
            resultPosOnScreen.Y = y.Clamp(windowPos.Y + 10, windowSize.Y + windowPos.Y - size.Y - 10);;
            resultPosOnScreen.X = screenItemArea.Max.X.Clamp(windowPos.X + 10,
                                                             windowPos.X + windowSize.X - size.X - 10);
        }
        
        var resultPosOnWindow = resultPosOnScreen - ImGui.GetWindowPos();

        ImGui.SetCursorPos(resultPosOnWindow);

        
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 6);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 14);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4, 6));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(3, 6));

        ImGui.PushStyleColor(ImGuiCol.ChildBg, UiColors.BackgroundFull.Fade(0.5f).Rgba);
        ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, Color.Transparent.Rgba);

        if(ImGui.BeginChild(999, size, true,
                                   ImGuiWindowFlags.None | ImGuiWindowFlags.AlwaysUseWindowPadding 
                                   )) 
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

            var gotAMatch = _filter.MatchingSymbolUis.Count > 0 && !_filter.MatchingSymbolUis.Contains(_selectedSymbolUi);
            if (gotAMatch)
                _selectedSymbolUi = _filter.MatchingSymbolUis[0];

            if ((_selectedSymbolUi == null && EditorSymbolPackage.AllSymbolUis.Any()))
                _selectedSymbolUi = EditorSymbolPackage.AllSymbolUis.First();

            var projectNamespace = "user." + context.CompositionOp.Symbol.SymbolPackage.AssemblyInformation.Name + ".";
            var currentMainComposition = context.CompositionOp;
            var compositionNameSpace = currentMainComposition.Symbol.Namespace;

            foreach (var symbolUi in _filter.MatchingSymbolUis)
            {
                var symbolHash = symbolUi.Symbol.Id.GetHashCode();
                ImGui.PushID(symbolHash);
                {
                    var symbolNamespace = symbolUi.Symbol.Namespace;
                    var isRelevantNamespace = symbolNamespace.StartsWith("Lib.")
                                              || symbolNamespace.StartsWith("Types.")
                                              || symbolNamespace.StartsWith("Examples.lib.")
                                              || symbolNamespace.StartsWith(projectNamespace)
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
                    ImGui.PushStyleColor(ImGuiCol.Text, ColorVariations.OperatorLabel.Apply(color).Rgba);

                    var isSelected = symbolUi == _selectedSymbolUi;

                    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 14);
                    _selectedItemChanged |= ImGui.Selectable($"##Selectable{symbolHash.ToString()}", isSelected);
                    ImGui.PopStyleVar();

                    var isHovered = ImGui.IsItemHovered();
                    if (isHovered)
                    {
                        ImGui.SetNextWindowSize(new Vector2(300, 0));
                        ImGui.BeginTooltip();
                        OperatorHelp.DrawHelpSummary(symbolUi);
                        ImGui.EndTooltip();
                    }

                    if (ImGui.IsItemActivated())
                    {
                        CreateInstance(context, symbolUi.Symbol);
                    }

                    ImGui.SameLine();

                    ImGui.TextUnformatted(symbolUi.Symbol.Name);
                    ImGui.PopStyleColor(3);
                }
                ImGui.PopID();
            }
        }
        ImGui.EndChild();

        ImGui.PopStyleColor(2);
        ImGui.PopStyleVar(4);
    }

    private void CreateInstance(GraphUiContext context, Symbol symbol)
    {
        if (context.MacroCommand == null || PlaceholderItem == null)
        {
            Log.Warning("Macro command missing for insertion");
            return;
        }

        var parentSymbol = context.CompositionOp.Symbol;
        var parentSymbolUi = parentSymbol.GetSymbolUi();

        var addSymbolChildCommand = new AddSymbolChildCommand(parentSymbol, symbol.Id) { PosOnCanvas = PlaceholderItem.PosOnCanvas };
        context.MacroCommand.AddAndExecCommand(addSymbolChildCommand);

        // Select new node
        if (!parentSymbolUi.ChildUis.TryGetValue(addSymbolChildCommand.AddedChildId, out var newChildUi))
        {
            Log.Warning($"Unable to create new operator - failed to retrieve new child ui \"{addSymbolChildCommand.AddedChildId}\" " +
                        $"from parent symbol ui {parentSymbolUi.Symbol}");
            return;
        }

        var newSymbolChild = newChildUi.SymbolChild;
        var newInstance = context.CompositionOp.Children[newChildUi.Id];
        context.Selector.SetSelection(newChildUi, newInstance);

        // TODO: add preset selection...

        ParameterPopUp.NodeIdRequestedForParameterWindowActivation = newSymbolChild.Id;
        context.Layout.FlagAsChanged();

        Close(context);
    }

    private readonly SymbolFilter _filter = new();
    private bool _focusInputNextTime = true;
    private bool _selectedItemChanged;
    private MagGraphItem.Directions Orientation = MagGraphItem.Directions.Horizontal;

    private SymbolUi _selectedSymbolUi;
    private static readonly int _uiId = "DraftNode".GetHashCode();
}