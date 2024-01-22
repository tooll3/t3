using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using T3.Core.Logging;

namespace T3.Editor.Compilation;

internal static class Compiler
{
    static readonly Stopwatch _stopwatch = new();
    
    public static bool TryCompile(CsProjectFile projectFile, BuildMode buildMode, Verbosity verbosity = Verbosity.Quiet)
    {
        _stopwatch.Restart();
        var workingDirectory = projectFile.Directory;
        
        const string configurationArgFmt = "--configuration {0}";
        string buildModeName = buildMode == BuildMode.Debug ? "Debug" : "Release";
        var buildModeArg = string.Format(configurationArgFmt, buildModeName);


        const string command = "dotnet";
        string arguments = $"build \"{projectFile.FullPath}\" --nologo {buildModeArg} --verbosity {VerbosityArgs[verbosity]} --output \"{projectFile.GetBuildTargetDirectory(buildMode)}\"";
        
        var process = new Process
                          {
                              StartInfo = new ProcessStartInfo
                                              {
                                                  FileName = command,
                                                  Arguments = arguments,
                                                  WorkingDirectory = workingDirectory,
                                                  UseShellExecute = false,
                                                  RedirectStandardOutput = true
                                              }
                          };

        List<string> output = new(50);

        process.OutputDataReceived += (sender, args) =>
                                      {
                                          if (args.Data == null)
                                              return;
                                          
                                          Console.WriteLine(args.Data);
                                          output.Add(args.Data);
                                      };

        process.Start();
        process.BeginOutputReadLine();
        process.WaitForExit();
        
        Log.Info($"{projectFile.Name}: Build process took {_stopwatch.ElapsedMilliseconds} ms");

        if (process.ExitCode != 0)
        {
            return false;
        }
        
        bool success = false;
        foreach (var line in output)
        {
            if (line.Contains("Build succeeded"))
            {
                success = true;
                break;
            }
        }
        
        if (!success)
        {
            Log.Error($"{projectFile.Name}: Build failed based on output in {_stopwatch.ElapsedMilliseconds}");
            _stopwatch.Stop();
            return false;
        }
        
        success = projectFile.TryLoadAssembly(buildMode);
        
        _stopwatch.Stop();
        Log.Info($"{projectFile.Name}: Total build time took {_stopwatch.ElapsedMilliseconds} ms");

        return success;
    }
    

    public enum BuildMode
    {
        Debug,
        Release
    }
    
    public enum Verbosity { Quiet, Minimal, Normal, Detailed, Diagnostic }

    private static readonly FrozenDictionary<Verbosity, string> VerbosityArgs = new Dictionary<Verbosity, string>()
                                                                                    {
                                                                                        { Verbosity.Quiet, "q" },
                                                                                        { Verbosity.Minimal, "m" },
                                                                                        { Verbosity.Normal, "n" },
                                                                                        { Verbosity.Detailed, "d" },
                                                                                        { Verbosity.Diagnostic, "diag" }
                                                                                    }.ToFrozenDictionary();
}