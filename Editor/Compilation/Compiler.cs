#nullable enable
using System.Collections.Frozen;
using System.Diagnostics;
using System.IO;
using Main;

namespace T3.Editor.Compilation;

internal static class Compiler
{
    internal static void StopProcess()
    {
        lock (_processLock)
        {
            if (_processCommander == null)
                return;
            
            _processCommander.Close();
            _processCommander = null;
        }
    }
    
    private static readonly string _workingDirectory = Path.Combine(T3.Core.UserData.UserData.TempFolder, "CompilationWorkingDirectory");

    static Compiler()
    {
        Directory.CreateDirectory(_workingDirectory);
    }

    private static string GetCommandFor(in CompilationOptions compilationOptions)
    {
        var projectFile = compilationOptions.ProjectFile;

        var buildModeName = compilationOptions.BuildMode == BuildMode.Debug ? "Debug" : "Release";
        var targetDirectory = compilationOptions.TargetDirectory ?? projectFile.GetBuildTargetDirectory();
        
        var restoreArg = compilationOptions.RestoreNuGet ? "" : "--no-restore";
        
        // construct command
        const string fmt = "dotnet build '{0}' --nologo --configuration {1} --verbosity {2} --output '{3}' {4} --no-self-contained";
        return string.Format(fmt, projectFile.FullPath, buildModeName, VerbosityArgs[compilationOptions.Verbosity], targetDirectory, restoreArg);
    }

    private static bool Evaluate(ref string output, in CompilationOptions options)
    {
        var success = output.Contains("Build succeeded");
        if (!success)
        {
            Log.Error(output);
        }

        return success;
    }
    
    private readonly record struct CompilationOptions(CsProjectFile ProjectFile, BuildMode BuildMode, string? TargetDirectory, Verbosity Verbosity, bool RestoreNuGet);
    
    private static ProcessCommander<CompilationOptions>? _processCommander;
    private static readonly object _processLock = new();
    
    public static bool TryCompile(CsProjectFile projectFile, BuildMode buildMode, bool nugetRestore, string? targetDirectory = null, Verbosity verbosity = Verbosity.Quiet)
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();

        bool success;

        lock (_processLock)
        {
            if (_processCommander == null)
            {
                _processCommander = new ProcessCommander<CompilationOptions>(_workingDirectory, "Compilation: ");
                Log.Info("Compilation process started");
            }
            
            if (!_processCommander.TryBeginProcess(out var isRunning) && !isRunning)
            {
                Log.Error("Failed to start compilation process");
                return false;
            }
            
            Log.Info($"Compiling {projectFile.Name} in {buildMode} mode");

            var compilationOptions = new CompilationOptions(projectFile, buildMode, targetDirectory, verbosity, nugetRestore);
            var command = new Command<CompilationOptions>(GetCommandFor, Evaluate);

            success = _processCommander.TryCommand(command, compilationOptions, projectFile.Directory, true);
        }

        if (!success)
        {
            Log.Error($"{projectFile.Name}: Build failed in {stopwatch.ElapsedMilliseconds}ms");
            return false;
        }
        
        stopwatch.Stop();
        Log.Info($"{projectFile.Name}: Build succeeded in {stopwatch.ElapsedMilliseconds}ms");

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