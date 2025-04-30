using ImGuiNET;

using System.Diagnostics;


using T3.Editor.Gui.Styling;


namespace T3.Editor.Gui.Windows;

internal sealed class AboutWindow : Window
{
    internal AboutWindow()
    {
        Config.Title = "About & help";
    }

    protected override void DrawContent()
    {

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

        // Use a child window with proper sizing
        ImGui.BeginChild("content", new Vector2(0, 0), // Auto-size both dimensions
                         false,
                         ImGuiWindowFlags.AlwaysAutoResize
                         | ImGuiWindowFlags.NoBackground
                         | ImGuiWindowFlags.AlwaysUseWindowPadding);

        FormInputs.AddSectionHeader("TiXL");
       
        ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.55f, 1.0f),"v " + Program.VersionText);

        FormInputs.AddVerticalSpace(5);
  
        ImGui.TextWrapped("MIT license");
        FormInputs.AddVerticalSpace(5);
        ImGui.Separator();
        FormInputs.AddVerticalSpace(5);
        ImGui.PushFont(Fonts.FontSmall);
        ImGui.TextWrapped("Help:");

        const string githubUrl = "https://github.com/tooll3/t3", wikiUrl = "https://github.com/tooll3/t3/wiki", websiteUrl = "https://tixl.app", discordURL = "https://discord.gg/cGnYbWJz";
        
        if (ImGui.Button("Website"))
        {
            try
            {
                Process.Start(new ProcessStartInfo(websiteUrl) { UseShellExecute = true });
            }
            catch (Exception e)
            {
                T3.Core.Logging.Log.Warning($"Failed to open link: {e.Message}");
            }
        }
        if (ImGui.IsItemHovered())
            CustomComponents.TooltipForLastItem(websiteUrl);

        ImGui.SameLine();
        if (ImGui.Button("Wiki"))
        {
            try
            {
                Process.Start(new ProcessStartInfo(wikiUrl) { UseShellExecute = true });
            }
            catch (Exception e)
            {
                T3.Core.Logging.Log.Warning($"Failed to open link: {e.Message}");
            }
        }
        if (ImGui.IsItemHovered())
            CustomComponents.TooltipForLastItem(wikiUrl);
        
        ImGui.SameLine();
        if (ImGui.Button("Discord"))
        {
            try
            {
                Process.Start(new ProcessStartInfo(discordURL) { UseShellExecute = true });
            }
            catch (Exception e)
            {
                T3.Core.Logging.Log.Warning($"Failed to open link: {e.Message}");
            }
        }
        if (ImGui.IsItemHovered())
            CustomComponents.TooltipForLastItem(discordURL);
       

        FormInputs.AddVerticalSpace(5);
        ImGui.Separator();
        FormInputs.AddVerticalSpace(5);

        ImGui.TextWrapped("Source Code:");
        
        if (ImGui.Button("Github"))
        {
            try
            {
                Process.Start(new ProcessStartInfo(githubUrl) { UseShellExecute = true });
            }
            catch (Exception e)
            {
                T3.Core.Logging.Log.Warning($"Failed to open link: {e.Message}");
            }
        }
        if (ImGui.IsItemHovered())
            CustomComponents.TooltipForLastItem(githubUrl);

        ImGui.PopFont();
        ImGui.EndChild();
        ImGui.PopStyleVar();
    }



    internal override List<Window> GetInstances()
    {
        return new List<Window>();
    }
}