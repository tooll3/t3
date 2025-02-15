using ImGuiNET;
using T3.Core.Model;
using T3.Core.SystemUi;
using T3.Editor.Compilation;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using GraphUtils = T3.Editor.UiModel.Helpers.GraphUtils;

namespace T3.Editor.Gui.Graph.Dialogs;

internal sealed class NewProjectDialog : ModalDialog
{
    protected override void OnShowNextFrame()
    {
        _shareResources = true;
        _newName = "projectName";
        _userName = UserSettings.Config.UserName;
        _newNamespace = "experiments";
        _needsAutoFocus = true;
    }
        
    public void Draw()
    {
        DialogSize = new Vector2(550, 320);

        if (BeginDialog("Create new project"))
        {
            // Name and namespace
            string namespaceWarningText = null;
            bool namespaceCorrect = true;
            if(!GraphUtils.IsNamespaceValid(_newNamespace, true, out _))
            {
                namespaceCorrect = false;
                namespaceWarningText = "Namespace must be a valid and unique C# namespace";
            }
                
            FormInputs.AddStringInput("Namespace", ref _newNamespace, 
                                      warning: namespaceWarningText, 
                                      autoFocus: _needsAutoFocus, tooltip:"Namespace is used to group your projects and should be unique to you.");
            _needsAutoFocus = false;
                
            var warning = string.Empty;
            var nameCorrect = true;
            if (!GraphUtils.IsIdentifierValid(_newName))
            {
                warning = "Name must be a valid C# identifier.";
                nameCorrect = false;
            }   
            else if (_newName.Contains('.'))
            {
                warning = "Name must not contain dots.";
                nameCorrect = false;
            }
            else if (string.IsNullOrWhiteSpace(_newName))
            {
                nameCorrect = false;
            }
            else if(DoesProjectWithNameExists(_newName))
            {
                // Todo - can we actually just allow this provided the project namespaces are different?
                warning = "A project with this name already exists.";
                nameCorrect = false;
            }
                
            //ImGui.SetKeyboardFocusHere();
            
            FormInputs.AddStringInput("Name", ref _newName,
                                      tooltip: "Is used to identify your project. Must not contain spaces or special characters.",
                                      warning: warning);

            var allValid = namespaceCorrect && nameCorrect;
            var fullName = $"{_userName}.{_newNamespace}.{_newName}";
            FormInputs.SetCursorToParameterEdit();                
            ImGui.TextColored(allValid ? UiColors.TextMuted : UiColors.StatusError, fullName);
            
                
            FormInputs.AddCheckBox("Share Resources", ref _shareResources, "Enabling this allows anyone with this package to reference shaders, " +
                                                                           "images, and other resources that belong to this package in other projects.\n" +
                                                                           "It is recommended that you leave this option enabled.");

            if (_shareResources == false)
            {
                ImGui.TextColored(UiColors.StatusWarning, "Warning: there is no way to change this without editing the project code at this time.");
            }
                
            if (CustomComponents.DisablableButton(label: "Create",
                                                  isEnabled: allValid,
                                                  enableTriggerWithReturn: false))
            {
                if (ProjectSetup.TryCreateProject(fullName, _shareResources, out var project))
                {
                    T3Ui.Save(false); // todo : this is probably not needed
                    ImGui.CloseCurrentPopup();

                    //GraphWindow.TryOpenPackage(project, false);
                    Log.Warning("Not implemented yet.");
                    BlockingWindow.Instance.ShowMessageBox($"Project \"{project.DisplayName}\"created successfully! It can be opened from the project list.");
                }
                else
                {
                    var message = $"Failed to create project \"{_newName}\" in \"{_newNamespace}\".\n\n" +
                                  "This should never happen - please file a bug report.\n" +
                                  "Currently this error is unhandled, so you will want to manually delete the project from disk.";
                        
                    Log.Error(message);
                    BlockingWindow.Instance.ShowMessageBox(message, "Failed to create new project");
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }
                
            FormInputs.SetIndentToLeft();
            FormInputs.AddHint("Creates a new project. Projects are used to group operators and resources. " +
                               "You can find your project in \\Documents\\T3Projects\\");
                
            FormInputs.SetIndentToParameters();                

            EndDialogContent();
        }

        EndDialog();
    }
        
    private static bool DoesProjectWithNameExists(string name)
    {
        foreach (var package in SymbolPackage.AllPackages.Cast<EditorSymbolPackage>())
        {
            if (!package.HasHome)
                continue;
                
            var existingProjectName = package.DisplayName;
                
            if (string.Equals(existingProjectName, name))
                return true;
        }
            
        return false;
    }

    private string _newName = string.Empty;
    private string _newNamespace = string.Empty;
    private string _userName = string.Empty;
    private bool _shareResources = true;
    private bool _needsAutoFocus;
}