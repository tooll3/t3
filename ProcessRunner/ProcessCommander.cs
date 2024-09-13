using System.Diagnostics;
using System.Text;

namespace Main;

public class ProcessCommander<T>(IEnumerable<T> items, bool printOnlyAtEnd, bool logToFiles, string logPrefix = "") where T : ILoggable
{
    public bool Finished { get; private set; } = true;

    /// <summary>
    /// Runs a powershell session with the given commands and this commander's data
    /// </summary>
    /// <param name="commands"></param>
    /// <param name="useAsync">True to use fully async IO, false to use a dedicated thread to capture powershell output</param>
    /// <exception cref="InvalidOperationException">Raised if this process commander is already running</exception>
    public void RunUntilFinished(IEnumerable<Command<T>> commands, bool useAsync, float delayBetweenItemsSeconds = 0f)
    {
        if (!Finished)
            throw new InvalidOperationException("Commander is already running");

        Finished = false;
        var process = CreatePowershellProcess();

        process.Start();
        process.Exited += (sender, args) => { };

        if (useAsync)
        {
            process.OutputDataReceived += OnOutputAsync;
            process.ErrorDataReceived += OnErrorAsync;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        Task.Run(() =>
        {
            _resetEvent.Reset();
            WriteCompleteMessage(process);
            _resetEvent.WaitOne(); // wait for response

            T? currentData = default;
            Console.WriteLine("Starting commands");
            var currentCommands = new Queue<Command<T>>();
            using var dataQueue = items.GetEnumerator();
            while (!process.HasExited)
            {
                if (currentCommands.TryDequeue(out var cmd))
                {
                    if(delayBetweenItemsSeconds > 0f)
                        Thread.Sleep((int)(delayBetweenItemsSeconds * 1000));
                    
                    var cmdString = cmd.GetCommand(currentData!);
                    if (cmdString == null)
                    {
                        WriteToConsole($" --- Failed to generate command for {currentData}");
                        currentCommands.Clear();
                        continue;
                    }

                    WriteToProcess(process, cmdString);
                    Thread.Sleep(100); // wait for command to start before writing completion signal
                    WriteCompleteMessage(process);

                    _resetEvent.WaitOne(); // wait for response

                    var previousOutput = _previousOutput;
                    var success = cmd.Evaluator(ref previousOutput, currentData!);

                    if (!success)
                    {
                        // return to working directory
                        WriteToProcess(process, $"cd '{ProcessCommands.WorkingDirectory}'");
                        
                        
                        // log failure to console
                        var response = previousOutput.Length < 256 ? previousOutput : previousOutput[^255..];

                        var failureLog =
                            $"Command failed for {currentData!.Name}: COMMAND: '{cmdString}' --> RESPONSE: '{response}'"; 
                        WriteToConsole(failureLog, true);
                        currentCommands.Clear();
                        
                        // create file to denote failure
                        try
                        {
                            var filePath = ProcessCommands.GetErrorFilePath(currentData, logPrefix);
                            using var writer = File.CreateText(filePath);
                            writer.WriteLine(currentData.ToString());
                            writer.WriteLine("--------------------------------------");
                            writer.WriteLine($"\nCommand: {cmdString}");
                            writer.WriteLine($"Response:\n{previousOutput}");
                            writer.Close();
                        }
                        catch (Exception ex)
                        {
                            WriteToConsole(ex.Message, true);
                        }
                    }

                    continue;
                }

                if (dataQueue.MoveNext())
                {
                    currentData = dataQueue.Current;
                    lock (_previousConsoleOutputSb)
                    {
                        _previousConsoleOutputSb.Clear();
                    }

                    foreach (var command in commands)
                    {
                        currentCommands.Enqueue(command);
                    }

                    Console.WriteLine($"Enqueued {currentCommands.Count} commands for {currentData}");

                    if (logToFiles)
                    {
                        InitializeLoggingFor(currentData);
                    }

                    continue;
                }

                ExitProcess(process);
                break;
            }

            if (dataQueue.MoveNext())
            {
                WriteToConsole("Not all data was processed", true);
            }
        });

        process.WaitForExit();

        if (printOnlyAtEnd)
        {
            lock (_totalConsoleOutput)
                Console.WriteLine(_totalConsoleOutput);
        }

        Finished = true;
        _logFileWriter?.Close();
        _logFileWriter = null;
        Console.WriteLine("Process finished");
    }

    private void ExitProcess(Process process)
    {
        WriteToProcess(process, "exit");
        process.Close();
        process.Dispose();
    }

    private static Process CreatePowershellProcess()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "pwsh",
                //Arguments = "",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                WorkingDirectory = ProcessCommands.WorkingDirectory
            },
            EnableRaisingEvents = true,
        };
        return process;
    }

    private void InitializeLoggingFor(T data)
    {
        try
        {
            _logFileWriter?.Close();
            var path = Path.Combine(ProcessCommands.LogDirectory, $"{logPrefix}_{data.Name}.log");
            _logFileWriter = new StreamWriter(path, Encoding.Default, ProcessCommands.FileStreamOptions);
            _logFileWriter.WriteLine(data.ToString());
            _logFileWriter.WriteLine("--------------------------------------");
            _logFileWriter.WriteLine();
        }
        catch (Exception e)
        {
            _logFileWriter = null;
            Console.WriteLine($"Failed to initialize logging for {data}: {e}");
        }
    }

    private void ReadAndPrintOutput(string outputString, bool error)
    {
        WriteToConsole(outputString, error);
        outputString = ProcessCommands.RemoveAnsiEscapeSequencesRegex.Replace(outputString, "");
        lock (_previousConsoleOutputSb)
        {
            _previousConsoleOutputSb.Append(outputString);
        }

        if (!IsCommandComplete(outputString))
            return;

        TriggerResetEvent();
        return;

        void TriggerResetEvent()
        {
            lock (_previousConsoleOutputSb)
            {
                _previousOutput = _previousConsoleOutputSb.ToString();
                _previousConsoleOutputSb.Clear();
            }

            _previousOutput = _previousOutput
                .Replace(CompleteMessageCommand, "")
                .Replace(CompleteMessage, "");

            _resetEvent.Set();
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

    private void WriteToConsole(string message, bool isError = false)
    {
        if (printOnlyAtEnd)
        {
            lock (_totalConsoleOutput)
                _totalConsoleOutput.AppendLine(message);
        }
        else
        {
            if (isError)
                Console.Error.WriteLine(message);
            else
                Console.WriteLine(message);
        }

        try
        {
            _logFileWriter?.WriteLine(message);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Failed to write to log file: {e}");
        }
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
            Console.WriteLine($"Failed to write to process: {e.Message}");
        }
    }

    ~ProcessCommander()
    {
        // release unmanaged resources
        _resetEvent.Dispose();
        _logFileWriter?.Close();
    }

    private static void WriteCompleteMessage(Process process) =>
        WriteToProcess(process, CompleteMessageCommand);

    private static bool IsCommandComplete(string output)
    {
        var completeIndex = output.LastIndexOf(CompleteMessage, StringComparison.Ordinal);
        if (completeIndex == -1)
            return false;

        // check to see if it's part of the echo statement, rather than the response to the statement itself
        var echoIndex = output.LastIndexOf("echo", StringComparison.Ordinal);
        return echoIndex != completeIndex - 6;
    }

    private readonly AutoResetEvent _resetEvent = new(true);

    private const string CompleteMessageCommand = $"echo '{CompleteMessage}'";
    private const string CompleteMessage = "{c_m_d}";
    private readonly StringBuilder _totalConsoleOutput = new();
    private readonly StringBuilder _previousConsoleOutputSb = new();
    private string _previousOutput = string.Empty;
    private StreamWriter? _logFileWriter;
}