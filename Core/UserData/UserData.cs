using System;
using System.Diagnostics;
using System.IO;
using T3.Core.Compilation;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Serialization;

namespace T3.Core.UserData;

public static class UserData
{
    static UserData()
    {
        Directory.CreateDirectory(SettingsFolderInApplicationDirectory);
        Directory.CreateDirectory(SettingsFolder);
    }

    public static bool TryLoad(out string config, string relativeFileName)
    {
        var filePath = Path.Combine(SettingsFolder, relativeFileName);
        if (File.Exists(filePath))
        {
            try
            {
                config = File.ReadAllText(filePath);
                return true;
            }
            catch
            {
                Log.Info($"User data file {filePath} could not be loaded.");
            }
        }

        var defaultsFilePath = Path.Combine(SettingsFolderInApplicationDirectory, relativeFileName);
        if (File.Exists(defaultsFilePath))
        {
            try
            {
                config = File.ReadAllText(defaultsFilePath);

                try
                {
                    var directory = Path.GetDirectoryName(filePath);
                    Directory.CreateDirectory(directory!);
                    File.WriteAllText(filePath, config);
                }
                catch (Exception e)
                {
                    Log.Error($"User data file {filePath} could not be saved. Error: {e}");
                }

                return true;
            }
            catch (Exception e)
            {
                Log.Info($"User data file {defaultsFilePath} could not be loaded. Error: {e}");
            }
        }

        config = string.Empty;
        return false;
    }

    public static bool CanLoad(string relativeFileName)
    {
        var filePath = Path.Combine(SettingsFolder, relativeFileName);
        if (File.Exists(filePath))
        {
            return true;
        }

        var defaultFilePath = Path.Combine(SettingsFolderInApplicationDirectory, relativeFileName);
        return File.Exists(defaultFilePath);
    }

    public static string SettingsFolderInApplicationDirectory => Path.Combine(RuntimeAssemblies.CoreDirectory!, ".t3");
    public static readonly string SettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "T3");
}