using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using System.Runtime.InteropServices;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Graph.Interaction;
using T3.Gui.InputUi;
using T3.Gui.Styling;
using T3.Gui.TypeColors;
using T3.Gui.Windows;
using UiHelpers;

namespace T3.Gui.UiHelpers
{
    /// <summary>
    /// Shows a tree of all defined symbols sorted by namespace 
    /// </summary>
    public static class SymbolTreeMenu
    {
        public static void Draw()
        {
            TreeNode.PopulateCompleteTree();
            DrawNodesRecursively(TreeNode);
        }

        private static void DrawNodesRecursively(NamespaceTreeNode subtree)
        {
            if (subtree.Name == "root")
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

        private static void DrawSymbolItem(Symbol symbol)
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

                if (ImGui.IsItemActive())
                {
                    if (ImGui.BeginDragDropSource())
                    {
                        if (_dropData == new IntPtr(0))
                        {
                            _guidSting = symbol.Id.ToString() + "|";
                            _dropData = Marshal.StringToHGlobalUni(_guidSting);
                            T3Ui.DraggingIsInProgress = true;
                        }

                        ImGui.SetDragDropPayload("Symbol", _dropData, (uint)(_guidSting.Length * sizeof(Char)));

                        ImGui.Button(symbol.Name + " (Dropping)");
                        ImGui.EndDragDropSource();
                    }
                }
                else if(ImGui.IsItemDeactivated())
                {
                    _dropData = new IntPtr(0);
                }

                ImGui.PopStyleColor(4);
            }
            ImGui.PopID();
        }

        private static readonly NamespaceTreeNode TreeNode = new NamespaceTreeNode("root");

        private static IntPtr _dropData = new IntPtr(0);
        private static string _guidSting;
    }
}