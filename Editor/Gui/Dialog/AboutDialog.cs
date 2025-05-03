#nullable enable
using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using ImGuiNET;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Dialog;

internal sealed class AboutDialog : ModalDialog
{
    internal void Draw()
    {
        DialogSize = new Vector2(500, 550) * T3Ui.UiScaleFactor;
        
        if (BeginDialog("About TiXL"))
        {
            FormInputs.AddSectionHeader("TiXL");
            ImGui.TextColored(new Vector4(1.0f, 0.2f, 0.55f, 1.0f), "v " + Program.VersionText);
            FormInputs.AddVerticalSpace(5);
            ImGui.Separator();
            
            FormInputs.AddSectionHeader("System Information");
            
            if (string.IsNullOrEmpty(_systemInfo))
            {
                UpdateSystemInfo(); // Populate system info if not already done
            }

            FormInputs.AddVerticalSpace(5);
            ImGui.TextWrapped(_systemInfo);
            FormInputs.AddVerticalSpace(5);
            
            
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

            CustomComponents.TooltipForLastItem("Copy system info for bug reports");
            EndDialogContent();
        }
        EndDialog();
    }
    
    private void UpdateSystemInfo()
    {
        try
        {
            var systemInfo = new StringBuilder();

            systemInfo.AppendLine($"Date: {DateTime.Now}");
            systemInfo.AppendLine($"TiXL version: {Program.VersionText}");
            systemInfo.AppendLine($"Language: {GetAppLanguage()}");
            systemInfo.AppendLine($"OS: {GetOperatingSystemInfo()}");
            systemInfo.AppendLine($"System language: {GetSystemLanguage()}");
            systemInfo.AppendLine($".NET runtime: {GetDotNetRuntimeVersion()}");
            systemInfo.AppendLine($".NET SDK: {GetDotNetSdkVersion()}");
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

    private static string GetGpuInformation()
    {
        var gpuList = new List<string>();

        try
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
            {
                
                foreach (var searchResult in searcher.Get())
                {
                    if (searchResult is not ManagementObject obj)
                        continue;
                    
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
    
    private string _systemInfo = string.Empty;


}