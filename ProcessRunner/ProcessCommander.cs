using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using T3.Core.Logging;

namespace Main;

internal static partial class ProcessUtils
{
    [GeneratedRegex(@"\x1B\[[0-?]*[ -/]*[@-~](?!\n)")]
    private static partial Regex RemoveAnsiEscapeSequences();

    private static readonly Regex RemoveAnsiEscapeSequencesRegex = RemoveAnsiEscapeSequences();

    public static string RemoveAnsiEscapeSequences(string input) =>
        RemoveAnsiEscapeSequencesRegex.Replace(input, "");
}

public class ProcessCommander<T>(string workingDirectory, string logPrefix = "")
{
    private Process? _process;

    public bool TryBeginProcess(out bool isRunning)
    {
        if (_process != null)
        {
            isRunning = true;
            return false;
        }

        if (!TryCreatePowershellProcess(out _process))
        {
            isRunning = false;
            return false;
        }

        _process.Exited += (processObj, _) =>
                           {
                               if (processObj is not Process process)
                                   return;

                               Close(process, 0f);
                           };

        _process.OutputDataReceived += OnOutputAsync;
        _process.ErrorDataReceived += OnErrorAsync;
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        isRunning = true;
        return true;
    }

    private bool TryCreatePowershellProcess([NotNullWhen(true)] out Process? process)
    {
        // prefer cross-platform modern powershell
        process = CreateProcess("pwsh", _workingDirectory);
        
        const string crossPlatformFailureMessage = "Failed to start cross-platform powershell";
        const string legacyFailureMessage = "Failed to start legacy powershell";
        try
        {
            if (!process.Start())
            {
                Close(process, 0f);
                T3.Core.Logging.Log.Warning(crossPlatformFailureMessage);
            }
            else
            {
                T3.Core.Logging.Log.Info("Started cross-platform powershell");
            }
        }
        catch (Exception e)
        {
            T3.Core.Logging.Log.Error($"{crossPlatformFailureMessage}: {e.Message}");
        }
        
        // fallback to legacy powershell
        process = CreateProcess("powershell", _workingDirectory);
        try
        {
            if (!process.Start())
            {
                Close(process, 0f);
                process = null;
                T3.Core.Logging.Log.Error(legacyFailureMessage);
                return false;
            }
            
            T3.Core.Logging.Log.Info("Started legacy powershell");
        }
        catch (Exception e)
        {
            T3.Core.Logging.Log.Error($"{legacyFailureMessage}: {e.Message}");
            process = null;
            return false;
        }

        return true;
    }

    public void Close(float timeoutSeconds = 3f)
    {
        if (_process == null)
            return;

        Close(_process, timeoutSeconds);
    }

    private void Close(Process process, float timeoutSeconds)
    {
        if (!process.HasExited)
        {
            WriteToProcess(process, "exit");

            var timeoutMs = (int)(timeoutSeconds * 1000);
            if (process.WaitForExit(timeoutMs))
            {
                return;
            }
        }

        if (!process.HasExited)
        {
            process.Kill();
        }

        process.Close();
        process.Dispose();

        if (process != _process)
        {
            T3.Core.Logging.Log.Error("Unexpected process exited");
            return;
        }

        _process = null;
    }

    public bool TryCommand(Command<T> cmd, T currentData, out string response, string? inDirectory = null, bool suppressOutput = false)
    {
        if(_process == null || _process.HasExited)
        {
            response = "Process is not running";
            if(!suppressOutput)
                Log(response, true);
            return false;
        }
        
        return TryCommand(cmd, currentData, _process!, out response, inDirectory, suppressOutput);
    }

    private bool TryCommand(Command<T> cmd, T currentData, Process process, out string response, string? inDirectory = null, bool suppressOutput = false)
    {
        var cmdString = cmd.GetCommand(currentData);
        if (cmdString == null)
        {
            response = $"Failed to generate command for {currentData}";
            Log(response, true);
            return false;
        }

        lock (_commandLock)
        {
            if (process.HasExited)
            {
                response = "Process has exited unexpectedly";
                if(!suppressOutput)
                    Log(response, true);
                
                return false;
            }

            var pOutputSuppress = _suppressConsoleOutput;
            _suppressConsoleOutput = suppressOutput;
            if (inDirectory != null)
            {
                ChangeDirectory(inDirectory);
            }

            response = WriteToProcessAndWaitForResponse(process, cmdString);
            _suppressConsoleOutput = pOutputSuppress;
        }
        
        var success = cmd.Evaluator(ref response, currentData!);

        if (success)
            return true;

        // log failure to console

        if (!suppressOutput)
        {
            var failureLog = $"Command failed for {currentData!}: COMMAND: '{cmdString}' --> RESPONSE: '{response}'";
            Log(failureLog, true);
        }

        return false;
    }

    public void ChangeDirectory(string inDirectory)
    {
        lock (_commandLock)
        {
            if (_process is not { HasExited: false })
            {
                T3.Core.Logging.Log.Error("Process is not running");
                return;
            }

            if (_workingDirectory == inDirectory)
                return;

            var response = WriteToProcessAndWaitForResponse(_process, $"cd '{inDirectory}'");
            if (response != null && response.Contains("annot find path"))
            {
                T3.Core.Logging.Log.Error($"Failed to change directory to '{inDirectory}'");
            }
            else
            {
                _workingDirectory = inDirectory;
            }
        }
    }

    private static Process CreateProcess(string processName, string workingDirectory)
    {
        var process = new Process
                          {
                              StartInfo = new ProcessStartInfo
                                              {
                                                  FileName = processName,
                                                  //Arguments = "",
                                                  RedirectStandardOutput = true,
                                                  UseShellExecute = false,
                                                  CreateNoWindow = true,
                                                  RedirectStandardInput = true,
                                                  RedirectStandardError = true,
                                                  WorkingDirectory = workingDirectory
                                              },
                              EnableRaisingEvents = true,
                          };
        return process;
    }

    private void ReadAndPrintOutput(string outputString, bool error)
    {
        if(!_suppressConsoleOutput)
            Log(outputString, error);
        
        outputString = ProcessUtils.RemoveAnsiEscapeSequences(outputString);

        lock (_outputLock)
        {
            _previousConsoleOutputSb.AppendLine(outputString);

            if (!IsCommandComplete(outputString))
            {
                return;
            }

            // command is complete - trigger the waiting thread and wait for it to finish to continue
            _produceOutputResetEvent.Set();
            _consumeOutputResetEvent.WaitOne();

            _previousConsoleOutputSb.Clear();
        }

        return;

        static bool IsCommandComplete(string output)
        {
            var completeIndex = output.LastIndexOf(CompleteMessage, StringComparison.Ordinal);
            if (completeIndex == -1)
                return false;

            // check to see if it's part of the echo statement, rather than the response to the statement itself
            var echoIndex = output.LastIndexOf("echo", StringComparison.Ordinal);
            return echoIndex != completeIndex - 6;
        }
    }

    private void OnErrorAsync(object sender, DataReceivedEventArgs e) => CaptureOutput(e, true);
    private void OnOutputAsync(object sender, DataReceivedEventArgs e) => CaptureOutput(e, false);

    private void CaptureOutput(DataReceivedEventArgs e, bool error)
    {
        var data = e.Data;
        if (data == null)
            return;

        ReadAndPrintOutput(data, error);
    }

    private void Log(string message, bool isError = false)
    {
        _singleLogSb.Clear();
        _singleLogSb.Append(logPrefix).Append(message);
        message = _singleLogSb.ToString();

        if (isError)
            T3.Core.Logging.Log.Error(message);
        else
            T3.Core.Logging.Log.Info(message);
    }

    private static void WriteToProcess(Process process, string message)
    {
        try
        {
            process.StandardInput.Write(message);
            process.StandardInput.Write(process.StandardInput.NewLine);
        }
        catch (Exception e)
        {
            // ignored
            T3.Core.Logging.Log.Error($"Failed to write to process: {e.Message}");
        }
    }

    private string WriteToProcessAndWaitForResponse(Process process, string cmdString)
    {
        try
        {
            process.StandardInput.Write(cmdString);
            process.StandardInput.Write(process.StandardInput.NewLine);
            process.StandardInput.Flush();
                //Thread.Sleep(10);
            
            //process.StandardInput.Write(process.StandardInput.NewLine); 
            Thread.Sleep(100); // wait for command to start before writing completion signal
            process.StandardInput.Write(CompleteMessageCommand);
            process.StandardInput.Write(process.StandardInput.NewLine);
            process.StandardInput.Flush();
        }
        catch (Exception e)
        {
            // ignored
            var msg = $"Failed to write to process: {e.Message}";
            T3.Core.Logging.Log.Error(msg);
            _consumeOutputResetEvent.Set();
            return msg;
        }
        
        return GetLatestOutput();

        string GetLatestOutput()
        {
            _produceOutputResetEvent.WaitOne();

            var output = _previousConsoleOutputSb.ToString();
            _consumeOutputResetEvent.Set();

            return output
                  .Replace(CompleteMessageCommand, "")
                  .Replace(CompleteMessage, "");
        }
    }

    ~ProcessCommander()
    {
        // release unmanaged resources
        _produceOutputResetEvent.Dispose();
        _consumeOutputResetEvent.Dispose();
        
        if (_process != null)
        {
            Close(_process, 0f);
        }
    }

    private readonly AutoResetEvent _produceOutputResetEvent = new(false);
    private readonly AutoResetEvent _consumeOutputResetEvent = new(false);

    private const string CompleteMessageCommand = $"echo '{CompleteMessage}'";
    private const string CompleteMessage = "c(^_^c)"; // this can be anything unique, the shorter the better
    private readonly StringBuilder _previousConsoleOutputSb = new();
    private readonly StringBuilder _singleLogSb = new();
    private readonly object _commandLock = new();
    private readonly object _outputLock = new();
    private string _workingDirectory = workingDirectory;
    private bool _suppressConsoleOutput = false;
}
