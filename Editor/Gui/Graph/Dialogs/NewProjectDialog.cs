using System.Numerics;
using System.Text;
using ImGuiNET;
using T3.Editor.Compilation;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Graph.Dialogs
{
    public class NewProjectDialog : ModalDialog
    {
        public void Draw()
        {
            DialogSize = new Vector2(500, 300);

            if (BeginDialog("Create new project"))
            {
                // Name and namespace
                var username = UserSettings.Config.UserName;

                string namespaceWarningText = null;
                bool namespaceCorrect = true;
                if (!_newNamespace.StartsWith(username + '.'))
                {
                    namespaceCorrect = false;
                    namespaceWarningText = $"Namespace must be within the \"{username}\" namespace";
                }
                else if(!GraphUtils.IsNamespaceValid(_newNamespace))
                {
                    namespaceCorrect = false;
                    namespaceWarningText = "Namespace must be a valid C# namespace";
                }
                    
                FormInputs.AddStringInput("Namespace", ref _newNamespace, 
                                          warning: namespaceWarningText);
                
                var nameCorrect = GraphUtils.IsIdentifierValid(_newName);
                FormInputs.AddStringInput("Name", ref _newName,
                                          warning: !nameCorrect ? "Name must be a valid C# identifier" : null);
                
                if (CustomComponents.DisablableButton(label: "Create",
                                                      isEnabled: namespaceCorrect && nameCorrect,
                                                      enableTriggerWithReturn: false))
                {
                    ProjectSetup.CreateProject(_newName, _newNamespace);
                    T3Ui.Save(false);
                    ImGui.CloseCurrentPopup();
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

        private static string _newName = string.Empty;
        private static string _newNamespace = string.Empty;
    }
}