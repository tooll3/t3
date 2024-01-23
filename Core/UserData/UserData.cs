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

    public static bool TryLoadOrWriteToUser(string relativeFilePath, out string fileText)
    {
        var filePath = GetFilePath(relativeFilePath, UserDataLocation.User);
        if (File.Exists(filePath))
        {
            try
            {
                fileText = File.ReadAllText(filePath);
                return true;
            }
            catch
            {
                Log.Info($"User data file {filePath} could not be loaded.");
            }
        }

        var defaultsFilePath = GetFilePath(relativeFilePath, UserDataLocation.Defaults);
        if (File.Exists(defaultsFilePath))
        {
            try
            {
                fileText = File.ReadAllText(defaultsFilePath);

                try
                {
                    var directory = Path.GetDirectoryName(filePath);
                    Directory.CreateDirectory(directory!);
                    File.WriteAllText(filePath, fileText);
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

        fileText = string.Empty;
        return false;
    }
    
    public static string GetFilePath(string relativeFilePath, UserDataLocation location)
    {
        var filePath = Path.Combine(location == UserDataLocation.User 
                                        ? SettingsFolder 
                                        : SettingsFolderInApplicationDirectory, relativeFilePath);
        return filePath;
    }

    public static bool TrySave(string relativeFilePath, string fileContent, UserDataLocation location = UserDataLocation.User)
    {
        var filePath = GetFilePath(relativeFilePath, location);
        var directory = Path.GetDirectoryName(filePath);
        Directory.CreateDirectory(directory!);
        try
        {
            File.WriteAllText(filePath, fileContent);
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save file {filePath}: {e}");
            return false;
        }
    }

    public static bool CanLoad(string relativeFileName, UserDataLocation location, out string filePath)
    {
        filePath = GetFilePath(relativeFileName, location);
        return File.Exists(filePath);
    }

    public static bool TryLoad(string relativeFilePath, UserDataLocation location, out string fileContent, out string filePath)
    {
        var canLoad = CanLoad(relativeFilePath, location, out filePath);
        if (!canLoad)
        {
            fileContent = string.Empty;
            return false;
        }

        try
        {
            fileContent = File.ReadAllText(filePath);
            return true;
        } 
        catch
        {
            Log.Info($"User data file {filePath} could not be loaded.");
            fileContent = string.Empty;
            return false;
        }
    }

    public static bool CanLoad(string relativeFileName)
    {
        var filePath = Path.Combine(SettingsFolder, relativeFileName);
        if (File.Exists(filePath))
        {
            return true;
        }

        var defaultsFilePath = Path.Combine(SettingsFolderInApplicationDirectory, relativeFileName);
        return File.Exists(defaultsFilePath);
    }

    public enum UserDataLocation
    {
        Defaults,
        User
    }

    public static string SettingsFolderInApplicationDirectory => Path.Combine(RuntimeAssemblies.CoreDirectory!, ".t3");
    
    public static readonly string SettingsFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "T3", Process.GetCurrentProcess().ProcessName);
}