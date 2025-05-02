using ImGuiNET;
using System.Diagnostics;
using T3.Editor.Gui.Styling;
using System.Runtime.InteropServices;
using System.Text;
using System.Management;
using System.Globalization;
using System;

namespace T3.Editor.Gui.Windows;

internal sealed class AboutWindow : Window
{
    private readonly List<(string Label, string Url)> _helpLinks =
    [
        ("Website", "https://tixl.app"),
        ("Wiki", "https://github.com/tooll3/t3/wiki"),
    ];

    private readonly List<(string Label, string Url)> _sourceLinks =
    [
        ("Github", "https://github.com/tooll3/t3")
    ];

    private string _systemInfo = string.Empty; // Store system info for display

    internal AboutWindow()
    {
        Config.Title = "About & Help";
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
        ImGui.TextColored(new Vector4(1.0f, 0.2f, 0.55f, 1.0f), "v " + Program.VersionText);
        ImGui.PushFont(Fonts.FontSmall);
        FormInputs.AddVerticalSpace(5);
        ImGui.TextWrapped("MIT license");
        FormInputs.AddVerticalSpace(5);
        ImGui.Separator();
        FormInputs.AddVerticalSpace(5);

        

        // Display system information in a collapsible section
        if (ImGui.CollapsingHeader("System Information"))
        {
            if (string.IsNullOrEmpty(_systemInfo))
            {
                UpdateSystemInfo(); // Populate system info if not already done
            }
            FormInputs.AddVerticalSpace(5);
            ImGui.PushFont(Fonts.FontSmall);
            ImGui.TextWrapped(_systemInfo);
            FormInputs.AddVerticalSpace(5);
            // System information copy button
            if (ImGui.Button("Copy System Information"))
            {
                UpdateSystemInfo(); // Update system info and copy to clipboard
                ImGui.SetClipboardText(_systemInfo);
                ImGui.OpenPopup("SystemInfoCopied");
            }

            // Confirmation popup
            if (ImGui.BeginPopup("SystemInfoCopied"))
            {
                ImGui.Text("System information copied to clipboard!");
                ImGui.EndPopup();
            }

            if (ImGui.IsItemHovered())
            {
                CustomComponents.TooltipForLastItem("Copy system info for bug reports");
            }
            ImGui.PopFont();
        }

        FormInputs.AddVerticalSpace(5);
        ImGui.Separator();
        FormInputs.AddVerticalSpace(5);

        
        ImGui.TextWrapped("Help:");
        ImGui.SameLine();
        DrawLinkButtons(_helpLinks);

        FormInputs.AddVerticalSpace(5);
        ImGui.Separator();
        FormInputs.AddVerticalSpace(5);
        //FormInputs.DrawInputLabel("Source Code:");
       
        ImGui.TextWrapped("Code:");
        ImGui.SameLine();
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

    private void UpdateSystemInfo()
    {
        try
        {
            var systemInfo = new StringBuilder();

            // Current date
            systemInfo.AppendLine($"Date: {DateTime.Now}");

            // TiXL version
            systemInfo.AppendLine($"TiXL version: {Program.VersionText}");

            // System language
            var appLanguage = GetAppLanguage();
            systemInfo.AppendLine($"Language: {appLanguage}");

            // OS information
            var osVersion = GetOperatingSystemInfo();
            systemInfo.AppendLine($"OS: {osVersion}");

            // System language
            var systemLanguage = GetSystemLanguage();
            systemInfo.AppendLine($"System language: {systemLanguage}");

            // .NET runtime version
            var dotNetVersion = GetDotNetRuntimeVersion();
            systemInfo.AppendLine($".NET runtime: {dotNetVersion}");

            // .NET SDK version
            var dotNetSdkVersion = GetDotNetSdkVersion();
            systemInfo.AppendLine($".NET SDK: {dotNetSdkVersion}");

            // GPU information
            var gpuInfo = GetGpuInformation();
            systemInfo.AppendLine($"GPU: {gpuInfo}");

            _systemInfo = systemInfo.ToString();
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to gather system information: {e.Message}");
            _systemInfo = "Failed to gather system information.";
        }
    }

    private static string GetOperatingSystemInfo()
    {
        var osDescription = RuntimeInformation.OSDescription;
        var osArchitecture = RuntimeInformation.OSArchitecture.ToString();

        return $"{osDescription} ({osArchitecture})";
    }

    private static string GetSystemLanguage()
    {
        try
        {
            var currentCulture = CultureInfo.CurrentUICulture;
            return $"{currentCulture.EnglishName}\nKeyboard layout:{currentCulture.KeyboardLayoutId} ({currentCulture.Parent}) ";
        }
        catch (Exception)
        {
            return "Unknown";
        }
    }

    private static string GetAppLanguage()
    {
        try
        {
            var appCulture = CultureInfo.CurrentCulture;
            return $"{appCulture.EnglishName} ";
        }
        catch (Exception)
        {
            return "Unknown";
        }
    }

    private static string GetDotNetRuntimeVersion()
    {
        return RuntimeInformation.FrameworkDescription;
    }

    private static string GetDotNetSdkVersion()
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "dotnet";
            process.StartInfo.Arguments = "--version";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(output))
            {
                return output;
            }
        }
        catch (Exception)
        {
            // Silently fail if we can't get the SDK version
        }

        return "Not found";
    }

    private static string GetGpuInformation()
    {
        var gpuList = new List<string>();

        try
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    var name = obj["Name"]?.ToString() ?? "Unknown";
                    var driverVersion = obj["DriverVersion"]?.ToString() ?? "Unknown";
                    var gpuDetails = $"{name}\nDriver version: {driverVersion}";
                    gpuList.Add(gpuDetails);
                }
            }

            return string.Join(", ", gpuList);
        }
        catch (Exception)
        {
            // Silently fail if we can't get GPU information
        }

        return "Unknown";
    }

    internal override List<Window> GetInstances()
    {
        return [];
    }
}