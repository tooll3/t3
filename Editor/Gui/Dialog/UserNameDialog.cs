using System;
using ImGuiNET;
using T3.Core.Logging;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Dialog
{
    public class UserNameDialog : ModalDialog
    {
        private string _userName;

        public void Draw()
        {
            if (BeginDialog("Edit nickname"))
            {
                ImGui.PushFont(Fonts.FontSmall);
                ImGui.TextUnformatted("Nickname");
                ImGui.PopFont();

                ImGui.SetNextItemWidth(150);

                if (ImGui.IsWindowAppearing())
                    ImGui.SetKeyboardFocusHere();

                _userName ??= UserSettings.Config.UserName;

                ImGui.InputText("##name", ref _userName, 255);

                CustomComponents
                   .HelpText("Tooll will use this to group your projects into a namespace.\n\n(This is a local setting only and not stored online.\n\nIt should be short and not contain spaces or special characters.");
                ImGui.Spacing();

                if (CustomComponents.DisablableButton("Rename", GraphUtils.IsValidUserName(_userName)))
                {
                    var eventArgs = new NameChangedEventArgs(UserSettings.Config.UserName, _userName);

                    // Change home (I.e. dashboard) namespace
                    //if(!SymbolRegistry.Entries.TryGetValue(UiModel.UiSymbolData.HomeSymbolId, out var homeSymbol))
                    //{
                    //    Log.Warning("Skipped setting home canvas namespace because symbol wasn't found");
                    //}
                    //else
                    //{
                    //    // todo : remove invalid characters
                    //    Log.Debug($"Moving home canvas to user.{UserSettings.Config.UserName}");
                    //    homeSymbol.Namespace = $"user.{UserSettings.Config.UserName}"
                    //    T3Ui.SaveAll();
                    //}

                    try
                    {
                        UserNameChanged?.Invoke(this, eventArgs);
                        UserSettings.Config.UserName = _userName;
                        UserSettings.Save();
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Error while renaming user {e}");
                        _userName = null;
                    }

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

        public event EventHandler<NameChangedEventArgs> UserNameChanged;

        public class NameChangedEventArgs : EventArgs
        {
            public NameChangedEventArgs(string newName, string oldName)
            {
                NewName = newName;
                OldName = oldName;
            }

            public string NewName { get; }
            public string OldName { get; }
        }
    }
}