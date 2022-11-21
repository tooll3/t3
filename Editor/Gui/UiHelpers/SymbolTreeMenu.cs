using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.Windows;

namespace T3.Editor.Gui.UiHelpers
{
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
                                : Color.Gray;

                ImGui.PushStyleColor(ImGuiCol.Button, ColorVariations.Operator.Apply(color).Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColorVariations.OperatorHover.Apply(color).Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, ColorVariations.OperatorInputZone.Apply(color).Rgba);
                ImGui.PushStyleColor(ImGuiCol.Text, ColorVariations.OperatorLabel.Apply(color).Rgba);

                if (ImGui.Button(symbol.Name))
                {
                    //_selectedSymbol = symbol;
                }
                
                ImGui.PopStyleColor(4);
                HandleDragAndDropForSymbolItem(symbol);

                if (SymbolAnalysis.InformationForSymbolIds.TryGetValue(symbol.Id, out var info))
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, T3Style.Colors.TextMuted.Rgba);
                    ListSymbolSetWithTooltip("  (needs {0}/", "  (",  info.RequiredSymbolIds);
                    ListSymbolSetWithTooltip("used by {0})  ", "NOT USED)  ",  info.DependingSymbolIds);
                    ImGui.PopStyleColor();
                }


                if (SymbolUiRegistry.Entries.TryGetValue(symbol.Id, out var symbolUi))
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

                if (ExampleSymbolLinking.ExampleSymbols.TryGetValue(symbol.Id, out var examples))
                {
                    ImGui.PushFont(Fonts.FontSmall);
                    ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
                    for (var index = 0; index < examples.Count; index++)
                    {
                        var exampleId = examples[index];
                        ImGui.SameLine();
                        ImGui.Button($"EXAMPLE");
                        HandleDragAndDropForSymbolItem(SymbolRegistry.Entries[exampleId]);
                    }

                    ImGui.PopStyleVar();
                    ImGui.PopFont();
                }

            }
            ImGui.PopID();
        }

        private static void ListSymbolSetWithTooltip(string setTitleFormat, string emptySetTitle, HashSet<Guid> symbolIdSet)
        {
            ImGui.PushID(setTitleFormat);
            ImGui.SameLine();
            
            if (symbolIdSet.Count == 0)
            {
                ImGui.TextUnformatted(emptySetTitle);
            }
            else
            {
                ImGui.TextUnformatted(string.Format(setTitleFormat, symbolIdSet.Count));
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ListSymbols(symbolIdSet);
                    ImGui.EndTooltip();
                }
            }
            
            ImGui.PopID();
        }

        private static void ListSymbols(HashSet<Guid> valueRequiredSymbolIds)
        {
            foreach (var required in valueRequiredSymbolIds
                                    .Select(rId =>
                                            {
                                                var rSymbol = SymbolRegistry.Entries[rId];
                                                return rSymbol.Namespace + ". " + rSymbol.Name;
                                            }).OrderBy(c => c))
            {
                ImGui.TextUnformatted(required);
            }
        }

        public static void HandleDragAndDropForSymbolItem(Symbol symbol)
        {
            if (ImGui.IsItemActive())
            {
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
                _dropData = new IntPtr(0);
            }
        }

        private static readonly NamespaceTreeNode _treeNode = new(NamespaceTreeNode.RootNodeId);

        private static IntPtr _dropData = new IntPtr(0);
        private static string _guidSting;
    }
}