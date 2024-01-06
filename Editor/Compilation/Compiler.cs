using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.UserData;
using T3.Editor.UiModel;

namespace T3.Editor.Compilation;

public static class Compiler
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

    public static CsProjectFile CreateNewProject(string projectName)
    {
        string destinationDirectory = Path.Combine(SymbolPackage.OperatorDirectoryName, "user", projectName);
        var defaultHomeDir = Path.Combine(UserData.RootFolder, "default-home");
        var files = Directory.EnumerateFiles(defaultHomeDir, "*");
        destinationDirectory = Path.GetFullPath(destinationDirectory);
        Directory.CreateDirectory(destinationDirectory);

        var dependenciesDirectory = Path.Combine(destinationDirectory, "dependencies");
        Directory.CreateDirectory(dependenciesDirectory);

        string placeholderDependencyPath = Path.Combine(dependenciesDirectory, "PlaceNativeDllDependenciesHere.txt");
        File.Create(placeholderDependencyPath).Dispose();

        const string namePlaceholder = "{{USER}}";
        const string guidPlaceholder = "{{GUID}}";
        string homeGuid = EditableSymbolPackage.HomeSymbolId.ToString();
        string csprojPath = null;
        foreach (var file in files)
        {
            string text = File.ReadAllText(file);
            text = text.Replace(namePlaceholder, projectName)
                       .Replace(guidPlaceholder, homeGuid);

            var destinationFilePath = Path.Combine(destinationDirectory, Path.GetFileName(file));
            destinationFilePath = destinationFilePath.Replace(namePlaceholder, projectName)
                                                     .Replace(guidPlaceholder, homeGuid);

            File.WriteAllText(destinationFilePath, text);

            if (destinationFilePath.EndsWith(".csproj"))
                csprojPath = destinationFilePath;
        }

        if (csprojPath == null)
        {
            Log.Error($"Could not find .csproj in {defaultHomeDir}");
            return null;
        }

        return new CsProjectFile(new FileInfo(csprojPath));
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