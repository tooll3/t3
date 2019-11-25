using System;
using System.Collections.Generic;
using ImGuiNET;
using T3.Core;
using T3.Core.Operator;
using T3.Gui.Graph.Interaction;
using T3.Gui.InputUi;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;

namespace T3.Gui.Graph.Dialogs
{
    public class AddInputDialog : ModalDialog
    {
        public void Draw(Instance compositionOp)
        {
            if (BeginDialog("Add parameter input"))
            {
                ImGui.SetNextItemWidth(120);
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Name");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(250);
                ImGui.InputText("##parameterName", ref _parameterName, 255);

                ImGui.SetNextItemWidth(80);
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Type:");
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
                    foreach (var typeUiPair in TypeUiRegistry.Entries)
                    {
                        var name = TypeNameRegistry.Entries[typeUiPair.Key];
                        var matchesSearch = TypeNameMatchesSearch(name);
                        
                        if (!matchesSearch)
                            continue;

                        if (ImGui.Button(name))
                        {
                            _selectedType = typeUiPair.Key;
                        }

                        ImGui.SameLine();
                    }
                    ImGui.PopFont();
                }

                ImGui.Spacing();

                var isValid = NodeOperations.IsNewSymbolNameValid(_parameterName) 
                              && _selectedType != null;
                if (CustomComponents.DisablableButton("Combine", isValid))
                {
                    // TODO: please implement
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

            if (Synonyms.ContainsKey(_searchFilter))
            {
                foreach(var alternative in Synonyms[_searchFilter])
                {
                    if (name.IndexOf(alternative, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }

            return false;
        }
        

        private string _parameterName = ""; // Initialize for ImGui edit
        private string _searchFilter = ""; // Initialize for ImGui edit
        private Type _selectedType;  
                                                     
        private static readonly Dictionary<string, string[]> Synonyms
            = new Dictionary<string, string[]>
              {
                  {"float", new[]{"Single",}},
              };
    }
}