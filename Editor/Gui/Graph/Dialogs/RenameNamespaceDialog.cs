using System.Text.RegularExpressions;
using ImGuiNET;
using T3.Core.SystemUi;
using T3.Editor.Compilation;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.SystemUi;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Dialogs
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
                ImGui.TextUnformatted("Namespace");
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
                    if (EditableSymbolProject.RenameNameSpaces(_node, _nameSpace, out var reason))
                    {
                        T3Ui.Save(false);
                    }
                    else
                    {
                        BlockingWindow.Instance.Show(reason, "Could not rename namespace");
                    }

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