#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Editor.SystemUi;

namespace T3.Editor.UiModel;

internal sealed partial class EditableSymbolProject
{
    public void SaveAll()
    {
        if (IsSaving)
        {
            Log.Error($"{CsProjectFile.Name}: Saving is already in progress.");
            return;
        }
        
        Log.Debug($"{CsProjectFile.Name}: Saving...");

        MarkAsSaving();
        WriteAllSymbolFilesOf(SymbolUis.Values);
        UnmarkAsSaving();
    }

    /// <summary>
    /// Note: This does NOT clean up 
    /// </summary>
    internal void SaveModifiedSymbols()
    {
        if (IsSaving)
        {
            Log.Error($"{CsProjectFile.Name}: Saving is already in progress.");
            return;
        }
        
        MarkAsSaving();

        var modifiedSymbolUis = SymbolUis
                               .Select(x => x.Value)
                               .Where(symbolUi => symbolUi.HasBeenModified)
                               .ToArray();

        if (modifiedSymbolUis.Length != 0)
        {
            Log.Debug($"{CsProjectFile.Name}: Saving {modifiedSymbolUis.Length} modified symbols...");

            WriteAllSymbolFilesOf(modifiedSymbolUis);
        }

        UnmarkAsSaving();
    }

    private void OnSymbolAdded(string? path, Symbol symbol)
    {
        path ??= SymbolPathHandler.GetCorrectPath(symbol.Name, symbol.Namespace, Folder, CsProjectFile.RootNamespace, SymbolExtension);
        
        var id = symbol.Id;
        if (_filePathHandlers.TryGetValue(id, out var handler))
            return;
        handler = new SymbolPathHandler(symbol, path);
        _filePathHandlers[id] = handler;
        
        if(AutoOrganizeOnStartup)
            handler.AllFilesReady += CorrectFileLocations;
    }

    private void OnSymbolUpdated(Symbol symbol)
    {
        var filePathHandler = _filePathHandlers[symbol.Id];

        if (symbol != filePathHandler.Symbol)
        {
            throw new Exception("Symbol mismatch when updating symbol files");
        }
        
        filePathHandler.UpdateFromSymbol();
    }

    /// <summary>
    /// Removal is a feature unique to editable projects - all others are assumed to be read-only and unchanging
    /// </summary>
    /// <param name="id">Id of the symbol to be removed</param>
    private void OnSymbolRemoved(Guid id)
    {
        Symbols.Remove(id, out var symbol);
        SymbolRegistry.EntriesEditable.Remove(id, out var publicSymbol);
        
        Debug.Assert(symbol != null);
        Debug.Assert(symbol == publicSymbol);
        
        SymbolUis.Remove(id, out _);
        SymbolUiRegistry.EntriesEditable.Remove(id, out _);

        Log.Info($"Removed symbol {symbol.Name}");
    }

    private static Action<SymbolPathHandler> CorrectFileLocations => handler =>
                                                                     {
                                                                         handler.AllFilesReady -= CorrectFileLocations;
                                                                         handler.UpdateFromSymbol();
                                                                     };

    protected override void OnSymbolUiLoaded(string? path, SymbolUi symbolUi)
    {
        var symbol = symbolUi.Symbol;
        symbolUi.ForceUnmodified = false;
        path ??= SymbolPathHandler.GetCorrectPath(symbol.Name, symbol.Namespace, Folder, CsProjectFile.RootNamespace, SymbolUiExtension);
        var filePathHandler = _filePathHandlers[symbol.Id];
        filePathHandler.UiFilePath = path;
    }

    protected override void OnSourceCodeLocated(string path, Guid guid)
    {
        if (_filePathHandlers.TryGetValue(guid, out var filePathHandler))
        {
            filePathHandler.SourceCodePath = path;
        }
        else
        {
            Log.Error($"No file path handler found for {guid}");
        }
    }

    private void WriteAllSymbolFilesOf(IEnumerable<SymbolUi> symbolUis)
    {
        foreach (var symbolUi in symbolUis)
        {
            SaveSymbolFile(symbolUi);
        }
    }

    private void SaveSymbolFile(SymbolUi symbolUi)
    {
        var symbol = symbolUi.Symbol;
        var id = symbol.Id;
        var pathHandler = _filePathHandlers[id];

        if (!pathHandler.TryCreateDirectory())
        {
            Log.Error($"Could not create directory for symbol {symbol.Id}");
            return;
        }

        pathHandler.UpdateFromSymbol();

        try
        {
                
            var sourceCodePath = pathHandler.SourceCodePath;
            if (sourceCodePath != null)
                WriteSymbolSourceToFile(id, sourceCodePath);
            else
                throw new Exception($"{CsProjectFile.Name}: No source code path found for symbol {id}");

            var symbolPath = pathHandler.SymbolFilePath ??= SymbolPathHandler.GetCorrectPath(symbol, this);
            SaveSymbolDefinition(symbol, symbolPath);
            pathHandler.SymbolFilePath = symbolPath;
                
            var uiFilePath = pathHandler.UiFilePath ??= SymbolPathHandler.GetCorrectPath(symbolUi, this);
            WriteSymbolUi(symbolUi, uiFilePath);
            pathHandler.UiFilePath = uiFilePath;
                
            #if DEBUG
                string debug = $"{CsProjectFile.Name}: Saved [{symbol.Name}] to:\nSymbol: \"{symbolPath}\"\nUi: \"{uiFilePath}\"\nSource: \"{sourceCodePath}\"\n";
                Log.Debug(debug);
            #endif
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save symbol {id}\n{e}");
        }
    }

    private static void WriteSymbolUi(SymbolUi symbolUi, string uiFilePath)
    {
        using var sw = new StreamWriter(uiFilePath, SaveOptions);
        using var writer = new JsonTextWriter(sw);

        writer.Formatting = Formatting.Indented;
        SymbolUiJson.WriteSymbolUi(symbolUi, writer);

        symbolUi.ClearModifiedFlag();
    }

    private void SaveSymbolDefinition(Symbol symbol, string filePath)
    {
        using var sw = new StreamWriter(filePath, SaveOptions);
        using var writer = new JsonTextWriter(sw);
        writer.Formatting = Formatting.Indented;
        SymbolJson.WriteSymbol(symbol, writer);
    }

    private void WriteSymbolSourceToFile(Guid id, string sourcePath)
    {
        if(!_pendingSource.Remove(id, out var sourceCode))
            return;

        using var sw = new StreamWriter(sourcePath, SaveOptions);
        sw.Write(sourceCode);
    }

    private void MarkAsSaving()
    {
        Interlocked.Increment(ref _savingCount);
        _csFileWatcher.EnableRaisingEvents = false;
    }

    private void UnmarkAsSaving()
    {
        Interlocked.Decrement(ref _savingCount);
        _csFileWatcher.EnableRaisingEvents = true;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs args)
    {
        MarkAsNeedingRecompilation();
    }

    private void OnFileRenamed(object sender, RenamedEventArgs args)
    {
        EditorUi.Instance.ShowMessageBox($"File {args.OldFullPath} renamed to {args.FullPath}. Please do not do this while the editor is running.");
        _needsCompilation = true;
    }

    

    public override bool TryGetSourceCodePath(Symbol symbol, out string? path)
    {
        if (_filePathHandlers.TryGetValue(symbol.Id, out var filePathInfo))
        {
            path = filePathInfo.SourceCodePath;
            return path != null;
        }

        path = null;
        return false;
    }

    public override void LocateSourceCodeFiles()
    {
        MarkAsSaving();
        base.LocateSourceCodeFiles();
        UnmarkAsSaving();
    }

    public static bool IsSaving => Interlocked.Read(ref _savingCount) > 0 || CheckCompilation(out _);
    private static long _savingCount;
    static readonly FileStreamOptions SaveOptions = new() { Mode = FileMode.Create, Access = FileAccess.ReadWrite };

    private readonly ConcurrentDictionary<Guid, SymbolPathHandler> _filePathHandlers = new();
    private const bool AutoOrganizeOnStartup = false;

    private sealed class CodeFileWatcher : FileSystemWatcher
    {
        public CodeFileWatcher(EditableSymbolProject project, FileSystemEventHandler onChange, RenamedEventHandler onRename) :
            base(project.Folder, "*.cs")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.DirectoryName;

            IncludeSubdirectories = true;
            Changed += onChange;
            Created += onChange;
            Renamed += onRename;
        }
    }
}