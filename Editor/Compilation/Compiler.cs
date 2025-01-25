#nullable enable
using System.Collections.Frozen;
using System.Diagnostics;
using System.IO;
using System.Text;
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
        if (output.Contains("Build succeeded")) return true;
        
        // print only errors
        const string searchTerm = "error";
        var searchTermSpan = searchTerm.AsSpan();
        for (int i = 0; i < output.Length; i++)
        {
            var newlineIndex = output.IndexOf('\n', i);
            var endOfLineIndex = newlineIndex == -1
                                     ? output.Length
                                     : newlineIndex;

            var span = output.AsSpan(i, endOfLineIndex - i);
            // if span contains "error"
            if (span.IndexOf(searchTermSpan) != -1)
            {
                _failureLogSb.Append(span).AppendLine();
            }

            i = endOfLineIndex;
        }

        output = _failureLogSb.ToString();
        _failureLogSb.Clear();
        return false;
    }
    
    private readonly record struct CompilationOptions(CsProjectFile ProjectFile, BuildMode BuildMode, string? TargetDirectory, Verbosity Verbosity, bool RestoreNuGet);
    
    private static ProcessCommander<CompilationOptions>? _processCommander;
    private static readonly object _processLock = new();
    private static readonly Stopwatch stopwatch = new();
    
    public static bool TryCompile(CsProjectFile projectFile, BuildMode buildMode, bool nugetRestore, string? targetDirectory = null, Verbosity verbosity = Verbosity.Detailed)
    {
        bool success;
        string logMessage;
        lock (_processLock)
        {
            stopwatch.Restart();
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

            if (!_processCommander.TryCommand(command, compilationOptions, out var response, projectFile.Directory, suppressOutput: false))
            {
                success = false;
                logMessage = $"{projectFile.Name}: Build failed in {stopwatch.ElapsedMilliseconds}ms:\n{response}";
            }
            else
            {
                success = true;
                logMessage = $"{projectFile.Name}: Build succeeded in {stopwatch.ElapsedMilliseconds}ms";
            }
        }

        if (!success)
        {
            Log.Error(logMessage);
        }
        else
        {
            Log.Info(logMessage);
        }
        
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
    
    private static readonly StringBuilder _failureLogSb = new();
}