using System;
using System.Linq;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Modification;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Graph.Dialogs;

public class RenameInputDialog : ModalDialog
{
    public void Draw()
    {
        if (BeginDialog("Rename input"))
        {
            DrawContent();
            EndDialogContent();
        }

        EndDialog();
    }

    private static void DrawContent()
    {
        var isWindowAppearing = ImGui.IsWindowAppearing();
        // ImGui.PushFont(Fonts.FontSmall);
        // ImGui.TextUnformatted("New name");
        // ImGui.PopFont();

        FormInputs.SetIndentToLeft();
        if (!SymbolRegistry.Entries.TryGetValue(_symbolId, out var symbol))
        {
            ImGui.TextUnformatted("invalid symbol");

            return;
        }
        FormInputs.AddHint($"Careful! This operation will modify the definition of {symbol.Name}.");
            if(symbol.Namespace.StartsWith("lib"))
            {
                FormInputs.AddHint("This is library Operator. Modifying it might prevent migrating your projects to future versions of Tooll");
                
            }

            FormInputs.SetIndentToParameters();
            var inputDef = symbol.InputDefinitions.FirstOrDefault(i => i.Id == _inputId);
        if (inputDef == null)
        {
            ImGui.TextUnformatted("invalid input");
            return;
        }

        if (isWindowAppearing)
        {
            _newInputName = inputDef.Name;
            _lastWarning = string.Empty;
        }

        // ImGui.SetNextItemWidth(150);

        //var warning = String.Empty;
        var isValid = GraphUtils.IsNewSymbolNameValid(_newInputName);
        if (!isValid)
        {
            _lastWarning = "Invalid name";
        }

        var changed = FormInputs.AddStringInput("New input name",
                                                ref _newInputName, "NewName",
                                                _lastWarning,
                                                "This is a C# class. It must be unique and\nnot include spaces or special characters");

        if (isValid && (isWindowAppearing || changed))
        {
            _lastWarning = null;
        }

        if (isWindowAppearing)
        {
            ImGui.SetKeyboardFocusHere();
        }
        
        FormInputs.ApplyIndent();

        if (CustomComponents.DisablableButton("Rename input", isValid))
        {
            // Fix simulate
            if (!InputsAndOutputs.RenameInput(symbol, _inputId, _newInputName, dryRun: true, out var newWarning))
            {
                _lastWarning = newWarning;
            }
            else
            {
                if (!InputsAndOutputs.RenameInput(symbol, _inputId, _newInputName, dryRun: false, out var warning))
                {
                    Log.Warning(warning);
                }

                ImGui.CloseCurrentPopup();
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            ImGui.CloseCurrentPopup();
        }
    }

    public void ShowNextFrame(Symbol symbol, Guid inputId)
    {
        ShowNextFrame();
        _symbolId = symbol.Id;
        _inputId = inputId;
    }

    private static Guid _symbolId; // avoid references
    private static Guid _inputId;
    private static string _newInputName = string.Empty;
    private static string _lastWarning = string.Empty;
}