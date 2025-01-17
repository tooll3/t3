using ImGuiNET;
using T3.Core.Model;
using T3.Core.SystemUi;
using T3.Editor.Compilation;
using T3.Editor.Gui.Graph.Window;
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
        _newName = string.Empty;
        _userName = UserSettings.Config.UserName;
        _newNamespace = _userName;
        _needsAutoFocus = true;
    }
        
    public void Draw()
    {
        DialogSize = new Vector2(550, 300);

        if (BeginDialog("Create new project"))
        {
            // Name and namespace
            string namespaceWarningText = null;
            bool namespaceCorrect = true;
            if (!_newNamespace.StartsWith(_userName) || _newNamespace.Length > _userName.Length && _newNamespace[_userName.Length] != '.')
            {
                namespaceCorrect = false;
                namespaceWarningText = $"Namespace must be within the \"{_userName}\" namespace";
            }
            else if(!GraphUtils.IsNamespaceValid(_newNamespace, true, out _))
            {
                namespaceCorrect = false;
                namespaceWarningText = "Namespace must be a valid and unique C# namespace";
            }
                
            FormInputs.AddStringInput("Namespace", ref _newNamespace, 
                                      warning: namespaceWarningText, 
                                      autoFocus: _needsAutoFocus, tooltip:"Namespace is used to group your projects and should be unique to you.");
            _needsAutoFocus = false;
                
            var warning = string.Empty;
            var nameCorrect = GraphUtils.IsIdentifierValid(_newName);
            if (!nameCorrect)
                warning = "Name must be a valid C# identifier.";   
                
            var isProjectNameUnique = !DoesProjectWithNameExists(_newName);
            if(!isProjectNameUnique)
                warning = "A project with this name already exists.";
                
            //ImGui.SetKeyboardFocusHere();
            FormInputs.AddStringInput("Name", ref _newName,
                                      tooltip: "Is used to identify your project. Must not contain spaces or special characters.",
                                      warning: warning);
                
            FormInputs.AddCheckBox("Share Resources", ref _shareResources, "Enabling this allows anyone with this package to reference shaders, " +
                                                                           "images, and other resources that belong to this package in other projects.\n" +
                                                                           "It is recommended that you leave this option enabled.");

            if (_shareResources == false)
            {
                ImGui.TextColored(UiColors.StatusWarning, "Warning: there is no way to change this without editing the project code at this time.");
            }
                

                
            if (CustomComponents.DisablableButton(label: "Create",
                                                  isEnabled: namespaceCorrect && nameCorrect && isProjectNameUnique,
                                                  enableTriggerWithReturn: false))
            {
                if (ProjectSetup.TryCreateProject(_newName, _newNamespace + '.' + _newName, _shareResources, out var project))
                {
                    T3Ui.Save(false); // todo : this is probably not needed
                    ImGui.CloseCurrentPopup();

                    GraphWindow.TryOpenPackage(project, false);
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