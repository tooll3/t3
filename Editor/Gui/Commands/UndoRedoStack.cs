using System.Collections.Generic;
using System.Text;

namespace T3.Editor.Gui.Commands;

public interface ICommand
{
    string Name { get; }
    bool IsUndoable { get; }
    void Undo();
    void Do();
}

internal static class CommandExtensions
{
    public static void LogError(this ICommand command, bool isUndo, string message, bool logStackTrace = true)
    {
        Log.Warning($"{command.GetType().Name} {(isUndo ? "Undo" : "Execute")}: {message}");
        if (logStackTrace)
        {
            Log.Debug(Environment.StackTrace);
        }
    }
}



public static class UndoRedoStack
{
    public static bool CanUndo => _undoStack.Count > 0;
    public static bool CanRedo => _redoStack.Count > 0;

    public static void AddAndExecute(ICommand command)
    {
        Add(command);

        command.Do();
    }

    public static void Add(ICommand command)
    {
        if (command.IsUndoable)
        {
            _undoStack.Push(command);
            _redoStack.Clear();
        }
        else
        {
            Clear();
        }
    }

    public static string GetNextUndoTitle()
    {
        if (!CanUndo)
            return null;

        return _undoStack.Peek().Name;
    }

    public static void Undo()
    {
        if (CanUndo)
        {
            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);
        }
    }

    public static void Redo()
    {
        if (CanRedo)
        {
            var command = _redoStack.Pop();
            command.Do();
            _undoStack.Push(command);
        }
    }

    public static void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }

    public static string GetUndoStackAsString()
    {
        var sb = new StringBuilder();
        var index = 0;
        foreach (var a in _undoStack)
        {
            sb.Append(a.Name);
            sb.Append('\n');
            index++;
            if (index > 20)
                break;
        }

        return sb.ToString();
    }

    private static readonly Stack<ICommand> _undoStack = new();
    private static readonly Stack<ICommand> _redoStack = new();

    public static Stack<ICommand> UndoStack => _undoStack;
    public static Stack<ICommand> RedoStack => _redoStack;
}