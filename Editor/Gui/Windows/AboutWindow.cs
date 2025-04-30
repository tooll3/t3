using ImGuiNET;
using System.Diagnostics;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.Windows;

internal sealed class AboutWindow : Window
{
    private readonly List<(string Label, string Url)> _helpLinks =
    [
        ("Website", "https://tixl.app"),
        ("Wiki", "https://github.com/tooll3/t3/wiki"),
        ("Discord", "https://discord.gg/YmSyQdeH3S")
    ];

    private readonly List<(string Label, string Url)> _sourceLinks =
    [
        ("Github", "https://github.com/tooll3/t3")
    ];

    internal AboutWindow()
    {
        MenuTitle = "About & Help";
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
        ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.55f, 1.0f), "v " + Program.VersionText);

        FormInputs.AddVerticalSpace(5);
        ImGui.TextWrapped("MIT license");
        FormInputs.AddVerticalSpace(5);
        ImGui.Separator();
        FormInputs.AddVerticalSpace(5);

        ImGui.PushFont(Fonts.FontSmall);
        ImGui.TextWrapped("Help:");

        DrawLinkButtons(_helpLinks);

        FormInputs.AddVerticalSpace(5);
        ImGui.Separator();
        FormInputs.AddVerticalSpace(5);

        ImGui.TextWrapped("Source Code:");

        DrawLinkButtons(_sourceLinks);

        ImGui.PopFont();
        ImGui.EndChild();
        ImGui.PopStyleVar();
    }

    private static void DrawLinkButtons(List<(string Label, string Url)> links)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(5, 0));

        var isFirst = true;
        foreach (var (label, url) in links)
        {
            if (!isFirst)
            {
                ImGui.SameLine();
            }

            if (ImGui.Button(label))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch (Exception e)
                {
                    Log.Warning($"Failed to open link: {e.Message}");
                }
            }

            if (ImGui.IsItemHovered())
            {
                CustomComponents.TooltipForLastItem(url);
            }

            isFirst = false;
        }

        ImGui.PopStyleVar();
    }

    internal override List<Window> GetInstances()
    {
        return [];
    }
}