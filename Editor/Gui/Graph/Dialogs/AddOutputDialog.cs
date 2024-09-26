using System.Diagnostics.CodeAnalysis;
using ImGuiNET;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Modification;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Dialogs
{
    public class AddOutputDialog : ModalDialog
    {
        public AddOutputDialog()
        {
            Flags = ImGuiWindowFlags.NoResize;
        }
        
        public void Draw(Symbol symbol)
        {
            if (BeginDialog("Add output"))
            {
                FormInputs.SetIndent(100);
                //ImGui.SetKeyboardFocusHere();
                _ = SymbolModificationInputs.DrawFieldInputs(symbol, ref _parameterName, ref _selectedType, out var isValid);
                FormInputs.AddCheckBox("Is time clip", ref _isTimeClip);
                
                FormInputs.ApplyIndent();
                if (CustomComponents.DisablableButton("Add", isValid))
                {
                    InputsAndOutputs.AddOutputToSymbol(_parameterName, _isTimeClip, _selectedType, symbol);
                    _parameterName = string.Empty;
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

        private bool _isTimeClip;
        private string _parameterName = string.Empty;
        private Type _selectedType;                                                                        
    }

    internal static class SymbolModificationInputs
    {
        public static bool DrawFieldInputs(Symbol symbol, [NotNullWhen(true)] ref string parameterName, [NotNullWhen(true)] ref Type selectedType, out bool isValid)
        {
            var changed = DrawFieldNameInput(symbol, ref parameterName, out var isNameValid);
            changed |= DrawTypeInput(ref selectedType, out var hasType);
            isValid = isNameValid && hasType;
            return changed;
        }

        public static bool DrawTypeInput(ref Type selectedType, out bool isValid)
        {
            FormInputs.DrawInputLabel("Type");
            
            var previous = selectedType;
            TypeSelector.Draw(ref selectedType);
            var changed = previous != selectedType;
            isValid = selectedType != null;
            return changed;
        }

        public static bool DrawFieldNameInput(Symbol symbol, ref string parameterName, out bool isValid)
        {
            var changed = FormInputs.AddStringInput("Name", ref parameterName);
            CustomComponents.HelpText("This is a C# field name. It must be unique and\nnot include spaces or special characters");

            isValid = GraphUtils.IsNewFieldNameValid(parameterName, symbol, out var reason);
            if (!isValid)
            {
                ImGui.TextColored(UiColors.StatusWarning, reason);
            }

            return changed;
        }

        public static bool DrawSymbolNameAndNamespaceInputs(ref string newTypeName, ref string newNamespace, EditableSymbolProject destinationProject,
                                                            out bool isValid)
        {
            var changed = DrawNamespaceInput(ref newNamespace, destinationProject, out var namespaceCorrect);
            //ImGui.SetCursorPosX(250 + 20); // Not sure how else to layout this
            ImGui.SameLine();
            ImGui.Spacing();
            changed |= DrawSymbolNameInput(ref newTypeName, newNamespace, destinationProject, true, out var symbolNameValid);
            isValid = namespaceCorrect && symbolNameValid;
            return changed;
        }

        private static void DrawLabel(string label)
        {
            ImGui.PushFont(Fonts.FontSmall);
            ImGui.TextUnformatted(label);
            ImGui.PopFont();
        }

        public static bool DrawNamespaceInput(ref string newNamespace, EditableSymbolProject destinationProject, out bool isValid)
        {
            var rootNamespace = destinationProject.CsProjectFile.RootNamespace;
            var isNamespaceCorrect = IsNamespaceCorrect(newNamespace, rootNamespace);

            DrawLabel("Namespace");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(250);
            var args = new InputWithTypeAheadSearch.Args<string>(Label: "##namespace2",
                                                                 Items: destinationProject.SymbolUis.Values
                                                                                          .Select(x => x.Symbol)
                                                                                          .Select(i => i.Namespace)
                                                                                          .Distinct()
                                                                                          .OrderBy(i => i),
                                                                 GetTextInfo: i => new InputWithTypeAheadSearch.Texts(i, i, null),
                                                                 Warning: !isNamespaceCorrect);
            
            var changed = InputWithTypeAheadSearch.Draw(args, ref newNamespace, out _);

            if (changed)
            {
                isNamespaceCorrect = IsNamespaceCorrect(newNamespace, rootNamespace);
            }
            
            isValid = isNamespaceCorrect;
            return changed;

            static bool IsNamespaceCorrect(string newNamespace, string rootNamespace)
            {
                return newNamespace != null && newNamespace.StartsWith(rootNamespace!) && GraphUtils.IsNamespaceValid(newNamespace, out _);
            }
        }

        public static bool DrawSymbolNameInput(ref string newTypeName, string destinationNamespace, SymbolPackage destinationPackage, bool focus, out bool isValid)
        {
            DrawLabel("Name");
            ImGui.SameLine();
            if (focus && ImGui.IsWindowAppearing())
                ImGui.SetKeyboardFocusHere();
            
            var changed = ImGui.InputText("##name", ref newTypeName, 50);
            CustomComponents.HelpText("This is a C# class name. It must be unique and\nnot include spaces or special characters");
            
            const string concatFmt = "{0}.{1}";
            CustomComponents.HelpText(string.Format(concatFmt, destinationNamespace, newTypeName));
            

            isValid = GraphUtils.IsNewSymbolNameValid(newTypeName, destinationNamespace, destinationPackage, out var reason);
            if (!isValid)
            {
                ImGui.TextColored(UiColors.StatusWarning, reason);
            }
            
            return changed;
        }
        
        

        public static bool DrawProjectDropdown(ref string nameSpace, ref EditableSymbolProject projectToCopyTo)
        {
            var projectChanged = CustomComponents.DrawProjectDropdown(ref projectToCopyTo);
            if(projectChanged && projectToCopyTo != null)
            {
                nameSpace = projectToCopyTo.CsProjectFile.RootNamespace;
            }
            return projectChanged;
        }
    }
}