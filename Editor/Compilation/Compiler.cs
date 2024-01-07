using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using T3.Core.Compilation;
using T3.Core.Logging;

namespace T3.Editor.Compilation;

internal static class Compiler
{
    public static bool TryCompile(CsProjectFile projectFile, BuildMode buildMode, Verbosity verbosity = Verbosity.Minimal)
    {
        var fullPath = projectFile.FullPath;
        var workingDirectory = projectFile.Directory;
        
        const string configurationArgFmt = "--configuration {0}";
        string buildModeName = buildMode == BuildMode.Debug ? "Debug" : "Release";
        var buildModeArg = string.Format(configurationArgFmt, buildModeName);


        const string command = "dotnet";
        string arguments = $"build \"{projectFile.FullPath}\" --nologo {buildModeArg} --verbosity {VerbosityArgs[verbosity]}";
        
        var process = new Process
                          {
                              StartInfo = new ProcessStartInfo
                                              {
                                                  FileName = command,
                                                  Arguments = arguments,
                                                  WorkingDirectory = workingDirectory,
                                                  UseShellExecute = true,
                                                  RedirectStandardOutput = true
                                              }
                          };

        List<string> output = new(50);

        process.OutputDataReceived += (sender, args) =>
                                      {
                                          if (args.Data == null)
                                              return;
                                          
                                          Log.Info(args.Data);
                                          output.Add(args.Data);
                                      };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            return false;
        }
        
        // todo - determine if compilation succeeded or not based on output of dotnet
        
        var assemblyFile = projectFile.GetBuildTargetPath(buildMode);
        if (!assemblyFile.Exists)
        {
            Log.Error($"Could not find assembly at \"{assemblyFile.FullName}\"");
            return false;
        }
        
        var gotAssembly = RuntimeAssemblies.TryLoadAssemblyInformation(assemblyFile.FullName, out var assembly);
        if (!gotAssembly)
        {
            Log.Error($"Could not load assembly at \"{assemblyFile.FullName}\"");
            return false;
        }
        
        projectFile.UpdateAssembly(assembly);
        return true;
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