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

            var symbolUi = symbol.GetSymbolUi();

            // var state = ParameterWindow.GetButtonStatesForSymbolTags(symbolUi.Tags);
            // if (CustomComponents.IconButton(Icon.Bookmark, Vector2.Zero, state))
            // {
            //     
            // }
            if(ParameterWindow.DrawSymbolTagsButton(symbolUi)) 
                symbolUi.FlagAsModified();
            
            ImGui.SameLine();
            
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
                
                if (!string.IsNullOrEmpty(symbolUi.Description))
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4,4));
                    ImGui.BeginTooltip();
                    ImGui.PushTextWrapPos(ImGui.GetFontSize() * 25.0f);
                    ImGui.TextUnformatted(symbolUi.Description);
                    ImGui.PopTextWrapPos();
                    ImGui.PopStyleVar();
                    ImGui.EndTooltip();
                }                
            }

            ImGui.PopStyleColor(4);
            HandleDragAndDropForSymbolItem(symbol);

            CustomComponents.ContextMenuForItem(drawMenuItems: () => CustomComponents.DrawSymbolCodeContextMenuItem(symbol), 
                                                title: symbol.Name,
                                                id: "##symbolTreeSymbolContextMenu");

            if (SymbolAnalysis.DetailsInitialized && SymbolAnalysis.InformationForSymbolIds.TryGetValue(symbol.Id, out var info))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
                //
                ListSymbolSetWithTooltip(250,Icon.Dependencies,"{0}", string.Empty, "requires...", info.RequiredSymbols);
                
                if (ListSymbolSetWithTooltip(300, Icon.Referenced, "{0}", " NOT USED",  "used by...", info.DependingSymbols))
                {
                    SymbolLibrary.SymbolUsageReference = symbol;
                }

                ImGui.PopStyleColor();
            }


            if (ExampleSymbolLinking.TryGetExamples(symbol.Id, out var examples))
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

    private static bool ListSymbolSetWithTooltip(float x, Icon icon, string setTitleFormat, string emptySetTitle, string toolTopTitle, HashSet<Symbol> symbolSet)
    {
        var activated = false;
        ImGui.PushID(icon.ToString());
        ImGui.SameLine(x,10);
        if (symbolSet.Count > 0)
        {
            icon.Draw();
            ImGui.SameLine(0, 5);
        }

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
                ImGui.TextUnformatted(toolTopTitle);
                FormInputs.AddVerticalSpace();
                ListSymbols(symbolSet);
                ImGui.EndTooltip();
            }

            if (ImGui.IsItemClicked())
            {
                activated = true;
            }
        }

        ImGui.PopID();
        //ImGui.SameLine();
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

        var comp = graphWindow.CompositionOp;

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

    internal static void HandleDragAndDropForSymbolItem(Symbol symbol)
    {
        if (IsSymbolCurrentCompositionOrAParent(symbol))
            return;

        DragHandling.HandleDragSourceForLastItem(DragHandling.SymbolDraggingId, symbol.Id.ToString(), "Create instance");

        if (!ImGui.IsItemDeactivated())
            return;
        
        var wasClick = ImGui.GetMouseDragDelta().Length() < 4;
        if (wasClick)
        {
            var window = GraphWindow.Focused;
            if (window == null)
            {
                Log.Error($"No focused graph window found");
            }
            else if (window.GraphCanvas.NodeSelection.GetSelectedChildUis().Count() == 1)
            {
                ConnectionMaker.InsertSymbolInstance(window, symbol);
            }
        }
    }

    private static readonly NamespaceTreeNode _treeNode = new(NamespaceTreeNode.RootNodeId);
}