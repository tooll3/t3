namespace Main;

public readonly record struct Command<T>(CommandGenerator<T> GetCommand, ResponseEvaluator<T> Evaluator);

public delegate string? CommandGenerator<T>(in T data);

public delegate bool ResponseEvaluator<TInput>(ref string response, in TInput data);