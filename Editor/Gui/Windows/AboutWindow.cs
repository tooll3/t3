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
    internal AboutWindow()
    {
        Config.Title = "About TiXL";
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

        FormInputs.AddSectionHeader("TiXL ");
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextDisabled.Rgba);
        
        FormInputs.AddSectionHeader("v." + Program.VersionText);
        ImGui.PopStyleColor();
        ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
        ImGui.TextWrapped($"{GetIdeName()}");
        ImGui.TextWrapped($"{DateTime.Now}");
        ImGui.PopStyleColor();
        FormInputs.AddVerticalSpace(10);
        ImGui.Separator();
        FormInputs.AddVerticalSpace(10);
        ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextDisabled.Rgba);
        FormInputs.AddSectionHeader("System Information");
        ImGui.PopStyleColor();
        FormInputs.AddVerticalSpace(5);
        ImGui.TextWrapped("MIT license");
        

        

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

            // IDE name (only in Debug mode)
            #if DEBUG
            systemInfo.AppendLine($"IDE: {GetIdeName()}");
            #endif

            // System language

            systemInfo.AppendLine($"Language: {GetAppLanguage()}");

            // OS information
            
            systemInfo.AppendLine($"OS: {GetOperatingSystemInfo()}");

            // System language
            
            systemInfo.AppendLine($"System language: {GetSystemLanguage()}");

            // .NET runtime version
            
            systemInfo.AppendLine($".NET runtime: {GetDotNetRuntimeVersion()}");

            // .NET SDK version
            
            systemInfo.AppendLine($".NET SDK: {GetDotNetSdkVersion()}");

            // GPU information
            
            systemInfo.AppendLine($"GPU: {GetGpuInformation()}");

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

    private static string GetGpuInformation(string infoType = "both")
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

                    string gpuDetails = infoType.ToLower() switch
                    {
                        "name" => name,
                        "driver" => driverVersion,
                        _ => $"{name}\nDriver version: {driverVersion}" // Default/both case
                    };

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
    private static string GetIdeName()
    {
        try
        {
            // Get the current process
            var currentProcess = Process.GetCurrentProcess();

            // Use WMI to find the parent process
            var query = $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {currentProcess.Id}";
            using var searcher = new ManagementObjectSearcher(query);
            var result = searcher.Get().Cast<ManagementObject>().FirstOrDefault();

            if (result != null)
            {
                var parentProcessId = Convert.ToInt32(result["ParentProcessId"]);
                using var parentProcess = Process.GetProcessById(parentProcessId);
                var processName = parentProcess.ProcessName.ToLower();

                // Map common IDE process names to user-friendly names
                return processName switch
                {
                    "devenv" => "Visual Studio",
                    "vsdebugconsole" => "Visual Studio",
                    "rider64" => "JetBrains Rider",
                    "vshost" => "Visual Studio (Debug Host)",
                    "msvsmon" => "Visual Studio Remote Debugger",
                    _ => parentProcess.ProcessName // Fallback to the raw process name
                };
            }
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to get IDE name: {e.Message}");
        }

        return "Unknown";
    }

    private static void DrawInformation(List<(string Label, string detail)> SystemInformation)
    {

    }

    internal override List<Window> GetInstances()
    {
        return [];
    }

    private List<(string Label, string Detail)> _systemInformation =
        [
            ("OS", $"{GetOperatingSystemInfo()}"),
            ("Language", $"{GetAppLanguage()}" ),
            ("System language", $"{GetSystemLanguage()}"),
            ("Keyboard layout", $"{CultureInfo.CurrentUICulture.KeyboardLayoutId}"),
            (".NET runtime", $"{GetDotNetRuntimeVersion()}"),
            (".NET SDK", $"{GetDotNetSdkVersion()}"),
            ("GPU", $"{GetGpuInformation("name")}"),
            ("Driver version", $"{GetGpuInformation("driver")}"),



        ];

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
}