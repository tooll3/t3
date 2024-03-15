using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Windows.Forms;
using ImGuiNET;
using Microsoft.VisualBasic.ApplicationServices;
using T3.Core.Operator;
using T3.Editor.Compilation;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Modification;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

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
                _namespaceBuilder.Clear();
                var username = UserSettings.Config.UserName;
                _namespaceBuilder.Append(username).Append('.');

                var incorrect = FormInputs.EnforceStringStart(_namespaceBuilder.ToString(), ref _newNamespace, true);
                FormInputs.AddStringInput("Namespace", ref _newNamespace, warning: incorrect ? $"Namespace must be within the {username} namespace" : null);
                FormInputs.AddStringInput("Name", ref _newName);
                
                _namespaceBuilder.Append(_newNamespace);
                
                if (CustomComponents.DisablableButton(label: "Create",
                                                      isEnabled: GraphUtils.IsIdentifierValid(_newName) && GraphUtils.IsNamespaceValid(_namespaceBuilder.ToString()),
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
        private static readonly StringBuilder _namespaceBuilder = new();
    }
}