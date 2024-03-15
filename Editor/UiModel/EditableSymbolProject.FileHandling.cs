#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Editor.Compilation;
using T3.Editor.SystemUi;
using SearchOption = System.IO.SearchOption;

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

    protected override void OnSymbolLoaded(string? path, Symbol symbol)
    {
        path ??= SymbolPathHandler.GetCorrectPath(symbol.Name, symbol.Namespace, Folder, CsProjectFile.RootNamespace, SymbolExtension);
        
        var id = symbol.Id;
        if (_filePathHandlers.TryGetValue(id, out var handler))
            return;
        handler = new SymbolPathHandler(symbol, path);
        handler.AllFilesReady += CorrectFileLocations;
        _filePathHandlers[id] = handler;
    }

    private static Action<SymbolPathHandler> CorrectFileLocations => handler =>
                                                                     {
                                                                         handler.AllFilesReady -= CorrectFileLocations;
                                                                         handler.UpdateFromSymbol();
                                                                     };

    protected override void OnSymbolUiLoaded(string? path, SymbolUi symbolUi)
    {
        var symbol = symbolUi.Symbol;
        path ??= SymbolPathHandler.GetCorrectPath(symbol.Name, symbol.Namespace, Folder, CsProjectFile.RootNamespace, SymbolUiExtension);
        var filePathHandler = _filePathHandlers[symbol.Id];
        filePathHandler.UiFilePath = path;
    }

    private void OnSourceCodeLocated(string path, Guid guid)
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

    protected override void OnSymbolUpdated(Symbol symbol)
    {
        var filePathHandler = _filePathHandlers[symbol.Id];

        if (symbol != filePathHandler.Symbol)
        {
            throw new Exception("Symbol mismatch when updating symbol files");
        }
        
        filePathHandler.UpdateFromSymbol();
    }

    private void WriteAllSymbolFilesOf(IEnumerable<SymbolUi> symbolUis)
    {
        foreach (var symbolUi in symbolUis)
        {
            var symbol = symbolUi.Symbol;
            var pathHandler = _filePathHandlers[symbol.Id];

            if (!pathHandler.TryCreateDirectory())
            {
                Log.Error($"Could not create directory for symbol {symbol.Id}");
                continue;
            }

            pathHandler.UpdateFromSymbol();

            try
            {
                
                var sourceCodePath = pathHandler.SourceCodePath;
                if (sourceCodePath != null)
                    WriteSymbolSourceToFile(symbolUi.Symbol, sourceCodePath);
                else
                    throw new Exception($"{CsProjectFile.Name}: No source code path found for symbol {symbolUi.Symbol.Id}");

                var symbolPath = pathHandler.SymbolFilePath ?? SymbolPathHandler.GetCorrectPath(symbol, this);
                SaveSymbolDefinition(symbolUi.Symbol, symbolPath);
                pathHandler.SymbolFilePath = symbolPath;
                
                var uiFilePath = pathHandler.UiFilePath ?? SymbolPathHandler.GetCorrectPath(symbolUi, this);
                WriteSymbolUi(symbolUi, uiFilePath);
                pathHandler.UiFilePath = uiFilePath;
                
                #if DEBUG
                string debug = $"{CsProjectFile.Name}: Saved [{symbol.Name}] to:\nSymbol: \"{symbolPath}\"\nUi: \"{uiFilePath}\"\nSource: \"{sourceCodePath}\"\n";
                Log.Debug(debug);
                #endif
            }
            catch (Exception e)
            {
                Log.Error($"Failed to save symbol {symbolUi.Symbol.Id}\n{e}");
            }
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

    private void WriteSymbolSourceToFile(Symbol symbol, string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(symbol.PendingSource))
            return;

        using var sw = new StreamWriter(sourcePath, SaveOptions);
        sw.Write(symbol.PendingSource);

        symbol.PendingSource = null;
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
        NeedsCompilation = true;
    }

    /// <summary>
    /// Looks for source codes in the project folder and subfolders and tries to find the symbol id in the source code
    /// </summary>
    public void LocateSourceCodeFiles()
    {
        MarkAsSaving();
        Directory.EnumerateFiles(Folder, $"*{SourceCodeExtension}", SearchOption.AllDirectories)
                 .AsParallel()
                 .ForAll(file =>
                         {
                             var streamReader = new StreamReader(file);

                             var guid = Guid.Empty;
                             while (!streamReader.EndOfStream)
                             {
                                 var line = streamReader.ReadLine();
                                 if (line == null)
                                     break;

                                 //todo: this is a bit hacky. Would it be better to look for "[Guid(" ?
                                 // todo - search for "[Guid("" while somehow ignoring whitespace
                                 var firstQuoteIndex = line.IndexOf('"');
                                 if (firstQuoteIndex == -1)
                                     continue;

                                 var secondQuoteIndex = line.IndexOf('"', firstQuoteIndex + 1);

                                 if (secondQuoteIndex == -1)
                                     continue;

                                 var stringSpan = line.AsSpan(firstQuoteIndex + 1, secondQuoteIndex - firstQuoteIndex - 1);

                                 if (!Guid.TryParse(stringSpan, out guid))
                                     continue;

                                 break;
                             }
                             
                             streamReader.Close();
                             streamReader.Dispose();

                             if (guid == Guid.Empty)
                                 return;
                             
                             OnSourceCodeLocated(file, guid);
                         });
        
        UnmarkAsSaving();
    }

    public bool TryGetSourceCodePath(Symbol symbol, out string? path)
    {
        if (_filePathHandlers.TryGetValue(symbol.Id, out var filePathInfo))
        {
            path = filePathInfo.SourceCodePath;
            return path != null;
        }

        path = null;
        return false;
    }

    public static bool IsSaving => Interlocked.Read(ref _savingCount) > 0 || EditableSymbolProject.CheckCompilation();
    private static long _savingCount;
    static readonly FileStreamOptions SaveOptions = new() { Mode = FileMode.Create, Access = FileAccess.ReadWrite };

    internal const string SourceCodeExtension = ".cs";
    private readonly ConcurrentDictionary<Guid, SymbolPathHandler> _filePathHandlers = new();

    private sealed class EditablePackageFsWatcher : FileSystemWatcher
    {
        public EditablePackageFsWatcher(EditableSymbolProject project, FileSystemEventHandler onChange, RenamedEventHandler onRename) :
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