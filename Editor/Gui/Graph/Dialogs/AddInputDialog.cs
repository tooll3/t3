using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.InputUi.VectorInputs;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Graph.Dialogs
{
    public class AddInputDialog : ModalDialog
    {
        public void Draw(Symbol symbol)
        {
            if (BeginDialog("Add parameter input"))
            {
                ImGui.SetNextItemWidth(120);
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Name:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(250);
                ImGui.InputText("##parameterName", ref _parameterName, 255);

                ImGui.SetNextItemWidth(80);
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Type:");
                ImGui.SameLine();

                ImGui.SetNextItemWidth(250);
                if (_selectedType != null)
                {
                    ImGui.Button(TypeNameRegistry.Entries[_selectedType] );
                    ImGui.SameLine();
                    if (ImGui.Button("x"))
                    {
                        _selectedType = null;
                    }
                }
                else
                {
                    ImGui.SetNextItemWidth(150);
                    ImGui.InputText("##namespace", ref _searchFilter, 255);

                    ImGui.PushFont(Fonts.FontSmall);
                    foreach (var (type, _) in TypeUiRegistry.Entries)
                    {
                        var name = TypeNameRegistry.Entries[type];
                        var matchesSearch = TypeNameMatchesSearch(name);
                        
                        if (!matchesSearch)
                            continue;

                        if (ImGui.Button(name))
                        {
                            _selectedType = type;
                        }

                        ImGui.SameLine();
                    }
                    ImGui.PopFont();
                }

                ImGui.Spacing();

                ImGui.SetNextItemWidth(80);
                ImGui.AlignTextToFramePadding();
                ImGui.Checkbox("Multi-Input", ref _multiInput);

                bool isValid = NodeOperations.IsNewSymbolNameValid(_parameterName) && _selectedType != null;
                if (CustomComponents.DisablableButton("Add", isValid))
                {
                    NodeOperations.AddInputToSymbol(_parameterName, _multiInput, _selectedType, symbol);
                    // NodeOperations.AddOutputToSymbol(_parameterName, _multiInput, _selectedType, symbol);
                    var symbolUi = SymbolUiRegistry.Entries[symbol.Id];
                    var inputUi = symbolUi.InputUis.Values.SingleOrDefault(i => i.InputDefinition.Name == _parameterName);
                    if (inputUi is FloatInputUi floatInputUi)
                    {
                        floatInputUi.Min = -1;
                        floatInputUi.Max = 42;
                    }
                }

                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }

                EndDialogContent();
            }

            EndDialog();
        }

        private bool TypeNameMatchesSearch(string name)
        {
            if (name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (!Synonyms.ContainsKey(_searchFilter))
                return false;

            return Synonyms[_searchFilter].Any(alternative => name.IndexOf(alternative, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        

        private string _parameterName = ""; // Initialize for ImGui edit
        private string _searchFilter = ""; // Initialize for ImGui edit
        private Type _selectedType;  
        private bool _multiInput;

        private static readonly Dictionary<string, string[]> Synonyms = new Dictionary<string, string[]>
                                                                        {
                                                                            { "float", new[] { "Single", } },
                                                                        };
    }
}