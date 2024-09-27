using System;
using System.IO;
using T3.Serialization;

namespace T3.Core.IO;

/// <summary>
/// Implements writing and reading configuration files 
/// </summary>
public class Settings<T> where T : class, new()
{
    public static T Config;
    public static T Defaults;

    protected Settings(string relativeFilePath, bool saveOnQuit)
    {
        if(saveOnQuit)
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        Defaults = new T();
        _filePath = Path.Combine(ConfigDirectory, relativeFilePath);
        Config = JsonUtils.TryLoadingJson<T>(_filePath) ?? new T();
        _instance = this;
    }

    private static void OnProcessExit(object sender, EventArgs e)
    {
        Save();
    }

    public static void Save()
    {
        JsonUtils.TrySaveJson(Config, _instance._filePath);
    }

    private static Settings<T> _instance;
    private readonly string _filePath;
    private static string ConfigDirectory => UserData.UserData.SettingsFolder;
}