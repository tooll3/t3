using System.Runtime.InteropServices;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.Windows;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.UiHelpers;

/// <summary>
/// Shows a tree of all defined symbols sorted by namespace.
/// This is used by <see cref="SymbolBrowser"/> and <see cref="T3Ui.DrawAppMenu"/>
/// </summary>
public static class SymbolTreeMenu
{
    public static void Draw()
    {
        _treeNode.PopulateCompleteTree();
        DrawNodesRecursively(_treeNode);
    }

    private static void DrawNodesRecursively(NamespaceTreeNode subtree)
    {
        if (subtree.Name == NamespaceTreeNode.RootNodeId)
        {
            DrawContent(subtree);
        }
        else
        {
            ImGui.PushID(subtree.Name);
            if (ImGui.BeginMenu(subtree.Name))
            {
                DrawContent(subtree);

                ImGui.EndMenu();
            }

            ImGui.PopID();
        }
    }

    private static void DrawContent(NamespaceTreeNode subtree)
    {
        foreach (var subspace in subtree.Children)
        {
            DrawNodesRecursively(subspace);
        }

        foreach (var symbol in subtree.Symbols)
        {
            DrawSymbolItem(symbol);
        }
    }

    public static void DrawSymbolItem(Symbol symbol)
    {
        ImGui.PushID(symbol.Id.GetHashCode());
        {
            var color = symbol.OutputDefinitions.Count > 0
                            ? TypeUiRegistry.GetPropertiesForType(symbol.OutputDefinitions[0]?.ValueType).Color
                            : UiColors.Gray;

            ImGui.PushStyleColor(ImGuiCol.Button, ColorVariations.OperatorBackground.Apply(color).Rgba);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColorVariations.OperatorBackgroundHover.Apply(color).Rgba);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ColorVariations.OperatorBackgroundHover.Apply(color).Rgba);
            ImGui.PushStyleColor(ImGuiCol.Text, ColorVariations.OperatorLabel.Apply(color).Rgba);

            if (ImGui.Button(symbol.Name))
            {
                //_selectedSymbol = symbol;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
            }

            ImGui.PopStyleColor(4);
            HandleDragAndDropForSymbolItem(symbol);

            CustomComponents.ContextMenuForItem(drawMenuItems: () => CustomComponents.DrawSymbolCodeContextMenuItem(symbol), 
                                                title: symbol.Name,
                                                id: "##symbolTreeSymbolContextMenu");

            if (SymbolAnalysis.DetailsInitialized && SymbolAnalysis.InformationForSymbolIds.TryGetValue(symbol.Id, out var info))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
                ListSymbolSetWithTooltip("  (needs {0}/", "  (", info.RequiredSymbols);
                if (ListSymbolSetWithTooltip("used by {0})  ", "NOT USED)  ", info.DependingSymbols))
                {
                    SymbolLibrary._symbolUsageReference = symbol;
                }

                ImGui.PopStyleColor();
            }

            var symbolUi = symbol.GetSymbolUi();
            {
                if (!string.IsNullOrEmpty(symbolUi.Description))
                {
                    ImGui.SameLine();
                    ImGui.TextDisabled("(?)");
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 25.0f);
                        ImGui.TextUnformatted(symbolUi.Description);
                        ImGui.PopTextWrapPos();
                        ImGui.EndTooltip();
                    }
                }
            }

            if (ExampleSymbolLinking.ExampleSymbolUis.TryGetValue(symbol.Id, out var examples))
            {
                ImGui.PushFont(Fonts.FontSmall);
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f * ImGui.GetStyle().Alpha);
                for (var index = 0; index < examples.Count; index++)
                {
                    var exampleSymbolUi = examples[index];
                    ImGui.SameLine();
                    ImGui.Button($"EXAMPLE");
                    HandleDragAndDropForSymbolItem(exampleSymbolUi.Symbol);
                }

                ImGui.PopStyleVar();
                ImGui.PopFont();
            }
                
        }
        ImGui.PopID();
    }

    private static bool ListSymbolSetWithTooltip(string setTitleFormat, string emptySetTitle, HashSet<Symbol> symbolSet)
    {
        var activated = false;
        ImGui.PushID(setTitleFormat);
        ImGui.SameLine();

        if (symbolSet.Count == 0)
        {
            ImGui.TextUnformatted(emptySetTitle);
        }
        else
        {
            ImGui.TextUnformatted(string.Format(setTitleFormat, symbolSet.Count));
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ListSymbols(symbolSet);
                ImGui.EndTooltip();
            }

            if (ImGui.IsItemClicked())
            {
                activated = true;
            }
        }

        ImGui.PopID();
        return activated;
    }

    private static void ListSymbols(HashSet<Symbol> valueRequiredSymbols)
    {
        foreach (var required in valueRequiredSymbols
                                .Select(rSymbol => rSymbol.Namespace + ". " + rSymbol.Name)
                                .OrderBy(c => c))
        {
            ImGui.TextUnformatted(required);
        }
    }

    private static bool IsSymbolCurrentCompositionOrAParent(Symbol symbol)
    {
        var graphWindow = GraphWindow.Focused;

        if (graphWindow == null)
            return true;

        var comp = graphWindow.Components.CompositionOp;

        if (comp.Symbol.Id == symbol.Id)
        {
            return true;
        }

        var instance = comp;
        while (instance != null)
        {
            if (instance.Symbol.Id == symbol.Id)
                return true;

            instance = instance.Parent;
        }

        return false;
    }

    public static void HandleDragAndDropForSymbolItem(Symbol symbol)
    {
        if (ImGui.IsItemActive())
        {
            if (IsSymbolCurrentCompositionOrAParent(symbol))
            {
                return;
            }

            if (ImGui.BeginDragDropSource())
            {
                if (_dropData == new IntPtr(0))
                {
                    _guidSting = symbol.Id + "|";
                    _dropData = Marshal.StringToHGlobalUni(_guidSting);
                    T3Ui.DraggingIsInProgress = true;
                }

                ImGui.SetDragDropPayload("Symbol", _dropData, (uint)(_guidSting.Length * sizeof(Char)));

                ImGui.Button(symbol.Name + " (creating instance)");
                ImGui.EndDragDropSource();
            }
        }
        else if (ImGui.IsItemDeactivated())
        {
            if (ImGui.GetMouseDragDelta().Length() < 4)
            {
                var window = GraphWindow.Focused;
                if (window == null)
                {
                    Log.Error($"No focused graph window found");
                }
                else if (window.Components.NodeSelection.GetSelectedChildUis().Count() == 1)
                {
                    ConnectionMaker.InsertSymbolInstance(window.Components, symbol);
                }
            }

            _dropData = new IntPtr(0);
        }
    }

    private static readonly NamespaceTreeNode _treeNode = new(NamespaceTreeNode.RootNodeId);

    private static IntPtr _dropData = new(0);
    private static string _guidSting;
}