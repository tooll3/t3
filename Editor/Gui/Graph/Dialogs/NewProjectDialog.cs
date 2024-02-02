using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using ImGuiNET;
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

            if (BeginDialog("Duplicate as new symbol"))
            {
                // Name and namespace
                FormInputs.AddStringInput("Namespace", ref _newNamespace);
                FormInputs.AddStringInput("Name", ref _newName);

                if (CustomComponents.DisablableButton(label: "Create",
                                                      isEnabled: GraphUtils.IsIdentifierValid(_newName) && GraphUtils.IsNamespaceValid(_newNamespace),
                                                      enableTriggerWithReturn: false))
                {
                    ProjectSetup.CreateOrMigrateProject(_newName, _newNamespace);
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