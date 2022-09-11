using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Graph.Interaction;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;

namespace T3.Gui.Graph.Dialogs
{
    public class UserNameDialog : ModalDialog
    {
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

                
                ImGui.InputText("##name", ref UserSettings.Config.UserName, 255);

                CustomComponents.HelpText("Tooll will use this to group your projects into a namespace.\n\n(This is a local setting only and not stored online.\n\nIt should be short and not contain spaces or special characters.");
                ImGui.Spacing();

                if (CustomComponents.DisablableButton("Rename", NodeOperations.IsValidUserName(UserSettings.Config.UserName)))
                {
                    UserSettings.Save();
                    
                    // Change dashboard namespace
                    if(!SymbolRegistry.Entries.TryGetValue(UiModel.DashboardSymbolId, out var dashboardSymbol))
                    {
                        Log.Warning("Skipped setting dashboard namespace because symbol wasn't found");
                    }
                    else
                    {
                        Log.Debug($"Moving dashboard to user.{UserSettings.Config.UserName}");
                        dashboardSymbol.Namespace = $"user.{UserSettings.Config.UserName}";
                        T3Ui.SaveAll();
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
    }
}