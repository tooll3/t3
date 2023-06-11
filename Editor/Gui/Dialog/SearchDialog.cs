using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.IO;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Dialog
{
    public class SearchDialog : ModalDialog
    {
        public SearchDialog()
        {
            DialogSize = new Vector2(400, 300);
            Padding = 4;
        }

        public void Draw()
        {
            if (BeginDialog("Search"))
            {
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
                if (!_isOpen)
                {
                    _justOpened = true;
                    _isOpen = true;
                }
                else
                {
                    _justOpened = false;
                }

                if (_justOpened)
                {
                    ImGui.SetKeyboardFocusHere();
                }

                var needsUpdate = _justOpened;
                FormInputs.SetIndent(0);
                needsUpdate |= FormInputs.AddStringInput("", ref _searchString, "Search", null, null, string.Empty);

                if (needsUpdate)
                {
                    UpdateResults();
                }

                if (ImGui.IsKeyPressed(ImGuiKey.Escape) && string.IsNullOrEmpty(_searchString))
                {
                    ImGui.CloseCurrentPopup();
                }

                DrawResultsList();
                ImGui.PopStyleVar();
                EndDialogContent();
            }
            else
            {
                _isOpen = false;
            }

            EndDialog();
        }

        private bool _isOpen = false;

        private void DrawResultsList()
        {
            var size = ImGui.GetContentRegionAvail();
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5, 5));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(10, 10));
            //var itemForHelpIsHovered = false;

            if (ImGui.BeginChildFrame(999, size))
            {
                if (ImGui.IsKeyReleased((ImGuiKey)Key.CursorDown))
                {
                    UiListHelpers.AdvanceSelectedItem(_matchingInstances, ref _selectedInstance, 1);
                    _selectedItemChanged = true;
                }
                else if (ImGui.IsKeyReleased((ImGuiKey)Key.CursorUp))
                {
                    UiListHelpers.AdvanceSelectedItem(_matchingInstances, ref _selectedInstance, -1);
                    _selectedItemChanged = true;
                }

                unsafe
                {
                    var clipperData = new ImGuiListClipper();
                    var listClipperPtr = new ImGuiListClipperPtr(&clipperData);

                    listClipperPtr.Begin(_matchingInstances.Count, ImGui.GetTextLineHeight());
                    while (listClipperPtr.Step())
                    {
                        for (var i = listClipperPtr.DisplayStart; i < listClipperPtr.DisplayEnd; ++i)
                        {
                            if (i < 0 || i >= _matchingInstances.Count)
                                continue;

                            DrawItem(_matchingInstances[i]);
                        }
                    }

                    listClipperPtr.End();
                }
            }

            ImGui.EndChildFrame();

            ImGui.PopStyleVar(2);
        }

        private void DrawItem(Instance instance)
        {
            var symbolHash = instance.Symbol.Id.GetHashCode();
            ImGui.PushID(symbolHash);
            {
                var symbolNamespace = instance.Symbol.Namespace;
                var isRelevantNamespace = symbolNamespace.StartsWith("lib.")
                                          || symbolNamespace.StartsWith("examples.lib.");

                var color = instance.Symbol.OutputDefinitions.Count > 0
                                ? TypeUiRegistry.GetPropertiesForType(instance.Symbol.OutputDefinitions[0]?.ValueType).Color
                                : Color.Gray;

                if (!isRelevantNamespace)
                {
                    color = color.Fade(0.4f);
                }

                ImGui.PushStyleColor(ImGuiCol.Header, ColorVariations.Operator.Apply(color).Rgba);

                var hoverColor = ColorVariations.OperatorHover.Apply(color).Rgba;
                hoverColor.W = 0.1f;
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, hoverColor);
                ImGui.PushStyleColor(ImGuiCol.HeaderActive, ColorVariations.OperatorInputZone.Apply(color).Rgba);
                ImGui.PushStyleColor(ImGuiCol.Text, ColorVariations.OperatorLabel.Apply(color).Rgba);
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(2, 4));

                var isSelected = instance == _selectedInstance;

                _selectedItemChanged |= ImGui.Selectable($"##Selectable{symbolHash.ToString()}", isSelected);
                //bool selectionChangedToThis = isSelected && _selectedItemChanged;

                var path = OperatorUtils.BuildIdPathForInstance(instance);
                var readablePath = string.Join('/', NodeOperations.GetReadableInstancePath(path));

                var isHovered = ImGui.IsItemHovered();
                // var hasMouseMoved = ImGui.GetIO().MouseDelta.LengthSquared() > 0;
                // if (hasMouseMoved && isHovered)
                // {
                //     _selectedInstance = instance;
                //     //_timeDescriptionSymbolUiLastHovered = DateTime.Now;
                //     _selectedItemChanged = true;
                // }
                // else 
                if (_selectedItemChanged && _selectedInstance == instance)
                {
                    UiListHelpers.ScrollToMakeItemVisible();
                    GraphWindow.GetPrimaryGraphWindow().GraphCanvas.OpenAndFocusInstance(path);
                    _selectedItemChanged = false;
                }

                if (ImGui.IsItemActivated())
                {
                    GraphWindow.GetPrimaryGraphWindow().GraphCanvas.OpenAndFocusInstance(path);
                    _selectedItemChanged = false;
                }

                ImGui.SameLine();

                ImGui.TextUnformatted(instance.Symbol.Name);
                ImGui.SameLine(0, 10);

                ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Fade(0.5f).Rgba);
                ImGui.TextUnformatted(readablePath);
                ImGui.PopStyleColor();

                // if (!string.IsNullOrEmpty(symbolNamespace))
                // {
                //     ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Fade(0.5f).Rgba);
                //     ImGui.Text(symbolNamespace);
                //     ImGui.PopStyleColor();
                //     ImGui.SameLine();
                // }

                // ImGui.NewLine();
                ImGui.PopStyleVar();
                ImGui.PopStyleColor(4);
            }
            ImGui.PopID();
        }

        private void FindAllChildren(Instance instance, Action<Instance> callback)
        {
            foreach (var child in instance.Children)
            {
                callback(child);
                //yield return child;
                FindAllChildren(child, callback);
            }
        }

        private void UpdateResults()
        {
            _matchingInstances.Clear();
            var mainComposition = T3Ui.UiModel.RootInstance;
            if (mainComposition == null)
                return;

            FindAllChildren(mainComposition,
                            instance =>
                            {
                                if (string.IsNullOrEmpty(_searchString)
                                    || instance.Symbol.Name.Contains(_searchString, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    _matchingInstances.Add(instance);
                                }
                            });

            _matchingInstances = _matchingInstances.OrderBy(r => r.Symbol.Name).ToList();
        }

        private List<Instance> _matchingInstances = new();
        private bool _justOpened;
        private static string _searchString;
        private Instance _selectedInstance;
        private bool _selectedItemChanged;
    }
}