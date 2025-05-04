#nullable enable
using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using ImGuiNET;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.App;


namespace T3.Editor.Gui.Dialog;

internal sealed class AboutDialog : ModalDialog
{
    internal void Draw()
    {
        DialogSize = new Vector2(500, 550) * T3Ui.UiScaleFactor;
        
        if (BeginDialog("About TiXL"))
        {
            FormInputs.AddSectionHeader("TiXL");
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);

            FormInputs.AddSectionHeader("v." + Program.VersionText);
            ImGui.PopStyleColor();
            ImGui.TextColored(UiColors.TextMuted, $"{dateTime}");
#if DEBUG
            ImGui.TextColored(UiColors.TextMuted, "IDE:");
            ImGui.SameLine();
            ImGui.Text($"{ideName}");
#endif
            
            ImGui.TextColored(UiColors.TextMuted, $"App language:");
            ImGui.SameLine();
            ImGui.Text($"{appLanguage}");
            //FormInputs.AddVerticalSpace(5);
            ImGui.Separator();
            
            FormInputs.AddSectionHeader("System Information");
            
            FormInputs.AddVerticalSpace(0);

            ImGui.TextColored(UiColors.TextMuted, "OS:" );
            ImGui.SameLine();
            ImGui.Text($"{operatingSystemInfo}");
            ImGui.TextColored(UiColors.TextMuted, "System language:");
            ImGui.SameLine();
            ImGui.Text($"{systemLanguage}");
            ImGui.TextColored(UiColors.TextMuted, "Keyboard layout:");
            ImGui.SameLine();
            ImGui.Text($"{keyboardLayout}");

            FormInputs.AddVerticalSpace(3);

            ImGui.TextColored(UiColors.TextMuted, ".NET Runtime:");
            ImGui.SameLine();
            ImGui.Text($"{dotNetRuntime}");
            ImGui.TextColored(UiColors.TextMuted, ".NET SDK:");
            ImGui.SameLine();
            ImGui.Text($"{dotNetSdk}");

            FormInputs.AddVerticalSpace(3);

            ImGui.TextColored(UiColors.TextMuted, "Graphics processing unit(s):"); 
            ImGui.Text($"{gpuInformation}");
            ImGui.Separator();

            //if (string.IsNullOrEmpty(_systemInfo))
            //{
            //    UpdateSystemInfo(); // Populate system info if not already done
            //}

            //FormInputs.AddVerticalSpace(5);
            //ImGui.TextWrapped(_systemInfo);
            //FormInputs.AddVerticalSpace(5);


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

            systemInfo.AppendLine($"{dateTime}");
            systemInfo.AppendLine($"TiXL version: {Program.VersionText}");
            #if DEBUG
            systemInfo.AppendLine($"IDE: {GetIdeName()}");
            #endif
            systemInfo.AppendLine($"Language: {GetAppLanguage()}");
            systemInfo.AppendLine($"OS: {GetOperatingSystemInfo()}");
            systemInfo.AppendLine($"System language: {GetSystemLanguage(englishName: true)}");
            systemInfo.AppendLine($"Keyboard Layout: {GetSystemLanguage(englishName: false)}");
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

    private static string GetSystemLanguage(bool englishName = true)
    {
        try
        {
            var currentCulture = CultureInfo.CurrentUICulture;
            return englishName
                ? currentCulture.EnglishName
                : $"{currentCulture.KeyboardLayoutId} ({currentCulture.Parent})";
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
        try
        {
            var sb = new StringBuilder();
            var activeGpu = ProgramWindows.ActiveGpu;

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
            {
                foreach (var searchResult in searcher.Get())
                {
                    if (searchResult is not ManagementObject obj)
                        continue;

                    var name = obj["Name"]?.ToString() ?? "Unknown";
                    if (name == activeGpu)
                        sb.AppendLine($"{name} (Active)");
                    else
                        sb.AppendLine(name);
                }
            }

            return sb.ToString();
        }
        catch (Exception)
        {
            return !string.IsNullOrEmpty(ProgramWindows.ActiveGpu)
                ? ProgramWindows.ActiveGpu + " (Active)"
                : "Unknown";
        }


    }

    // TODO: add DriverVersion to GetGpuInformation 
    //private static string GetGpuList(string infoType = "both")
    //{
    //    var gpuList = new List<string>();

    //    try
    //    {
    //        using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
    //        {
    //            foreach (ManagementObject obj in searcher.Get())
    //            {
    //                var name = obj["Name"]?.ToString() ?? "Unknown";
    //                var driverVersion = obj["DriverVersion"]?.ToString() ?? "Unknown";

    //                string gpuDetails = infoType.ToLower() switch
    //                {
    //                    "name" => name,
    //                    "driver" => driverVersion,
    //                    _ => $"{name}\nDriver version: {driverVersion}" // Default/both case
    //                };

    //                gpuList.Add(gpuDetails);
    //            }
    //        }

    //        return string.Join(", ", gpuList);
    //    }
    //    catch (Exception)
    //    {
    //        // Silently fail if we can't get GPU information
    //    }

    //    return "Unknown";
    //}

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
    private static readonly string ideName = GetIdeName();
    private static readonly string appLanguage = GetAppLanguage();
    private static readonly string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    private static readonly string operatingSystemInfo = GetOperatingSystemInfo();
    private static readonly string systemLanguage = GetSystemLanguage(englishName: true);
    private static readonly string keyboardLayout = GetSystemLanguage(englishName: false);
    private static readonly string dotNetRuntime = GetDotNetRuntimeVersion();
    private static readonly string dotNetSdk = GetDotNetSdkVersion();
    private static readonly string gpuInformation = GetGpuInformation();

    private string _systemInfo = string.Empty;


}