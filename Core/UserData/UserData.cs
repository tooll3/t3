using System;
using System.IO;

namespace T3.Core.UserData;

public static class UserData
{
    static UserData()
    {
        var processPath = Environment.ProcessPath;
        if (processPath != null)
        {
            var processDirectory = Path.GetDirectoryName(processPath);
            RootFolder = Path.Combine(processDirectory!, ".t3");
            Directory.CreateDirectory(RootFolder);
        }
        else
        {
            throw new Exception("Could not determine executable path");
        }
    }
    
    public static string RootFolder { get; }
}