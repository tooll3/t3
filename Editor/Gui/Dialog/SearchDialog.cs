using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using T3.SystemUi;

namespace T3.Editor.Gui.Dialog
{
    public class SearchDialog : ModalDialog
    {
        public SearchDialog()
        {
            DialogSize = new Vector2(500, 300);
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
                FormInputs.SetIndentToLeft();
                
                FormInputs.SetWidth(0.7f);
                needsUpdate |= FormInputs.AddStringInput("", ref _searchString, "Search", null, null, string.Empty);
                ImGui.SameLine();
                FormInputs.SetWidth(1f);
                needsUpdate |= FormInputs.AddEnumDropdown(ref _searchMode, "");
                FormInputs.ResetWidth();
                
                
                if (needsUpdate)
                {
                    UpdateResults();
                }


                var clickedOutside = ImGui.IsMouseClicked(ImGuiMouseButton.Left) 
                                     && !ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows| ImGuiHoveredFlags.AllowWhenBlockedByActiveItem );
                if (ImGui.IsKeyReleased(ImGuiKey.Enter) || ImGui.IsKeyReleased(ImGuiKey.Escape) || clickedOutside)
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

            if (ImGui.BeginChildFrame(999, size))
            {
                if (ImGui.IsKeyReleased((ImGuiKey)Key.CursorDown))
                {
                    UiListHelpers.AdvanceSelectedItem(_matchingInstances, ref _selectedInstance, 1);
                    _selectedItemChanged = true;
                    var index = _matchingInstances.IndexOf(_selectedInstance);
                    if(index == 0)
                        ImGui.SetScrollY(0);
                }
                else if (ImGui.IsKeyReleased((ImGuiKey)Key.CursorUp))
                {
                    UiListHelpers.AdvanceSelectedItem(_matchingInstances, ref _selectedInstance, -1);
                    _selectedItemChanged = true;
                    
                    var index = _matchingInstances.IndexOf(_selectedInstance);

                    if (index * ImGui.GetTextLineHeight() > ImGui.GetScrollY() + ImGui.GetContentRegionAvail().Y)
                    {
                        Log.Debug("Would scroll down");
                        ImGui.SetScrollY(ImGui.GetScrollY() + ImGui.GetContentRegionAvail().Y);
                    }
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
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(2, 4));

                var isSelected = instance == _selectedInstance;
                var hasBeenClicked = ImGui.Selectable($"##Selectable{symbolHash.ToString()}", isSelected);
                _selectedItemChanged |= hasBeenClicked; 

                var path = OperatorUtils.BuildIdPathForInstance(instance);
                var readablePath = string.Join(" / ", Structure.GetReadableInstancePath(path, false));
                
                if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    GraphWindow.GetPrimaryGraphWindow().GraphCanvas.OpenAndFocusInstance(path);
                    _selectedInstance = instance;
                    _selectedItemChanged = false;
                }
                else if (_selectedItemChanged && _selectedInstance == instance)
                {
                    UiListHelpers.ScrollToMakeItemVisible();
                    GraphWindow.GetPrimaryGraphWindow().GraphCanvas.OpenAndFocusInstance(path);
                    _selectedItemChanged = false;
                }


                ImGui.SameLine();

                ImGui.TextUnformatted(instance.Symbol.Name);
                ImGui.SameLine(0, 10);

                ImGui.PushStyleColor(ImGuiCol.Text, UiColors.Gray.Fade(0.5f).Rgba);
                ImGui.TextUnformatted(readablePath);
                ImGui.PopStyleColor();
                
                ImGui.PopStyleVar();
                ImGui.PopStyleColor(3);
            }
            ImGui.PopID();
        }

        private void FindAllChildren(Instance instance, Action<Instance> callback)
        {
            foreach (var child in instance.Children)
            {
                callback(child);
                if (_searchMode == SearchModes.Local)
                    continue;
                FindAllChildren(child, callback);
            }
        }

        private void UpdateResults()
        {
            _matchingInstances.Clear();
            
            if(string.IsNullOrEmpty(_searchString))
            {
                _matchingInstances.AddRange(NavigationHistory.GetPreviouslySelectedInstances());
                _selectedInstance = _matchingInstances.Count > 0 ? _matchingInstances[0] : null;
                
                return;
            }
            
            var composition = _searchMode switch
                                      {
                                          SearchModes.Global             => EditableSymbolProject.RootInstance,
                                          SearchModes.Local              => GraphWindow.GetMainComposition(),
                                          SearchModes.LocalAndInChildren => GraphWindow.GetMainComposition(),
                                          _                              => throw new ArgumentOutOfRangeException()
                                      };

            if (composition == null)
                return;

            FindAllChildren(composition,
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
        private SearchModes _searchMode = SearchModes.Local;

        private enum SearchModes
        {
            Local,
            LocalAndInChildren,
            Global,
        }
    }
}