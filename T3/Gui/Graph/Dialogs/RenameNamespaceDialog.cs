using System.Text.RegularExpressions;
using ImGuiNET;
using T3.Gui.Graph.Interaction;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;
using T3.Gui.Windows;

namespace T3.Gui.Graph.Dialogs
{
    public class RenameNamespaceDialog : ModalDialog
    {
        public void Draw(NamespaceTreeNode subtreeNodeToRename)
        {
            if (BeginDialog("Rename namespace"))
            {
                var dialogJustOpened = _node == null;
                if (dialogJustOpened)
                {
                    _node = subtreeNodeToRename;
                    _nameSpace = _node.GetAsString();
                }

                ImGui.PushFont(Fonts.FontSmall);
                ImGui.Text("Namespace");
                ImGui.PopFont();

                ImGui.SetNextItemWidth(150);

                if (ImGui.IsWindowAppearing())
                    ImGui.SetKeyboardFocusHere();

                ImGui.InputText("##name", ref _nameSpace, 255);

                CustomComponents.HelpText("Careful now. This operator might affect a lot of operator definitions");
                ImGui.Spacing();

                if (CustomComponents.DisablableButton(
                                                      "Rename",
                                                      !string.IsNullOrEmpty(_nameSpace) 
                                                      && Regex.IsMatch(_nameSpace, @"^[\d\w_\.]+$")
                                                      ))
                {
                    NodeOperations.RenameSpaceSpaces(_node, _nameSpace);
                    Close();
                }

                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    Close();
                }

                EndDialogContent();
            }

            EndDialog();
        }

        private static void Close()
        {
            ImGui.CloseCurrentPopup();
            _node = null;
        }

        private static NamespaceTreeNode _node;
        private static string _nameSpace;
    }
}