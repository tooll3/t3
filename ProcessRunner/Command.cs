namespace Main;

/// <summary>
/// The definition of a command given to the process (powershell)
/// </summary>
public readonly record struct Command<T>(CommandGenerator<T> GetCommand, ResponseEvaluator<T> Evaluator);

/// <summary>
/// Converts a piece of data into a command one would type into a powershell terminal.
/// </summary>
public delegate string? CommandGenerator<T>(in T data);

/// <summary>
/// Evaluates the response of a process to a given command data type. This is the terminal output
/// of the command once it has completed running.
/// Implementations should return true if the response passed in indicates success,
/// and return false if it indicates failure.
/// Changing the response string will change the terminal output (if enabled)
/// </summary>
public delegate bool ResponseEvaluator<TInput>(ref string response, in TInput data);
