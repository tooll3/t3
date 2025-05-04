using System.Windows.Forms;
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
        _newProjectName = "MyProject";
        _userName = UserSettings.Config.UserName;
        _newSubNamespace = "";
        _needsAutoFocus = true;
    }
        
    public void Draw()
    {
        DialogSize = new Vector2(550, 320);

        if (BeginDialog("Create new project"))
        {
            // Namespace
            string namespaceWarningText = null;
            bool namespaceCorrect = true;
            if(!GraphUtils.IsNamespaceValid(_newSubNamespace, true, out _))
            {
                namespaceCorrect = false;
                namespaceWarningText = "Namespace must be a valid and unique C# namespace";
            }
                
            FormInputs.AddStringInput("Namespace", ref _newSubNamespace, 
                                      warning: namespaceWarningText, 
                                      autoFocus: _needsAutoFocus, 
                                      tooltip:"An additional namespace withing your user area that can help to further group your projects.");
            _needsAutoFocus = false;
                
            // ProjectName
            var warning = string.Empty;
            var nameCorrect = true;
            
            if (!GraphUtils.IsIdentifierValid(_newProjectName))
            {
                warning = "Name must be a valid C# identifier.";
                nameCorrect = false;
            }   
            else if (_newProjectName.Contains('.'))
            {
                warning = "Name must not contain dots.";
                nameCorrect = false;
            }
            else if (string.IsNullOrWhiteSpace(_newProjectName))
            {
                nameCorrect = false;
            }
            else if(DoesProjectWithNameExists(_newProjectName))
            {
                // Todo - can we actually just allow this provided the project namespaces are different?
                warning = "A project with this name already exists.";
                nameCorrect = false;
            }
                
            //ImGui.SetKeyboardFocusHere();
            
            FormInputs.AddStringInput("Name", ref _newProjectName,
                                      "Is used to identify your project. Must not contain spaces or special characters.",
                                      warning);

            var allValid = namespaceCorrect && nameCorrect;

            var subNameSpaceWithSeparator = string.IsNullOrEmpty(_newSubNamespace)
                                                ? ""
                                                : _newSubNamespace + ".";
            
            var fullName = $"{_userName}.{subNameSpaceWithSeparator}{_newProjectName}";
            
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
                    //Log.Warning("Not implemented yet.");
                    //BlockingWindow.Instance.ShowMessageBox($"Project \"{project.DisplayName}\"created successfully! It can be opened from the project list.");
                    Log.Debug($"Project \"{project.DisplayName}\"created successfully! It can be opened from the project list.");
                }
                else
                {
                    var message = $"Failed to create project \"{_newProjectName}\" in \"{_newSubNamespace}\".\n\n" +
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
        foreach (var package in SymbolPackage.AllPackages)
        {
            if (package is not EditorSymbolPackage symbolPackage)
                continue;
            
            if (!symbolPackage.HasHome)
                continue;
            
            // TODO: it would be great with SymbolPackage would have a "Name" field
            if (DoesStringMatchesLastPathItem(name, symbolPackage.AssemblyInformation.Name))
            {
                return true;
            }
        }
            
        return false;
    }

    private static bool DoesStringMatchesLastPathItem(string name, ReadOnlySpan<char> path)
    {
        var lastDot = path.LastIndexOf('.');
        if (lastDot < 0) 
            return false;
        
        var lastSegment = path.Slice(lastDot + 1);
        return lastSegment.Equals(name, StringComparison.OrdinalIgnoreCase);
    }

    private string _newProjectName = string.Empty;
    private string _newSubNamespace = string.Empty;
    private string _userName = string.Empty;
    private bool _shareResources = true;
    private bool _needsAutoFocus;
}