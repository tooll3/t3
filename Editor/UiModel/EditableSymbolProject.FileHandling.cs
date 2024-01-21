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
using T3.Editor.SystemUi;
using SearchOption = System.IO.SearchOption;

namespace T3.Editor.UiModel;

internal sealed partial class EditableSymbolProject
{
    public void SaveAll()
    {
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

    private void WriteAllSymbolFilesOf(IEnumerable<SymbolUi> symbolUis)
    {
        foreach (var symbolUi in symbolUis)
        {
            var filePathFmt = BuildFilepathFmt(symbolUi.Symbol);
            var destinationFolder = Path.GetDirectoryName(filePathFmt);

            try
            {
                Directory.CreateDirectory(destinationFolder!);

                SaveSymbolDefinition(symbolUi.Symbol, filePathFmt);
                WriteSymbolSourceToFile(symbolUi.Symbol, filePathFmt);
                WriteSymbolUi(symbolUi, filePathFmt);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to save symbol {symbolUi.Symbol.Id}\n{e}");
            }
        }
    }

    private static void WriteSymbolUi(SymbolUi symbolUi, string filePathFmt)
    {
        var uiFilePath = string.Format(filePathFmt, SymbolUiExtension);

        MoveIfNecessary(symbolUi.UiFilePath, uiFilePath);
        symbolUi.UiFilePath = uiFilePath;

        using var sw = new StreamWriter(uiFilePath, SaveOptions);
        using var writer = new JsonTextWriter(sw);

        writer.Formatting = Formatting.Indented;
        SymbolUiJson.WriteSymbolUi(symbolUi, writer);

        symbolUi.ClearModifiedFlag();
    }

    private void SaveSymbolDefinition(Symbol symbol, string filePathFmt)
    {
        var filePath = string.Format(filePathFmt, SymbolExtension);

        MoveIfNecessary(symbol.SymbolFilePath, filePath);
        symbol.SymbolFilePath = filePath;

        using var sw = new StreamWriter(filePath, SaveOptions);
        using var writer = new JsonTextWriter(sw);
        writer.Formatting = Formatting.Indented;
        SymbolJson.WriteSymbol(symbol, writer);
    }

    private void WriteSymbolSourceToFile(Symbol symbol, string filePathFmt)
    {
        var sourcePath = string.Format(filePathFmt, SourceCodeExtension);
        if (_sourceCodeFiles.TryGetValue(symbol.Id, out var previousSourcePath))
        {
            MoveIfNecessary(previousSourcePath, sourcePath);
        }

        _sourceCodeFiles[symbol.Id] = sourcePath;

        if (string.IsNullOrWhiteSpace(symbol.PendingSource))
            return;

        using var sw = new StreamWriter(sourcePath, SaveOptions);
        sw.Write(symbol.PendingSource);

        symbol.PendingSource = null;
    }

    /// <summary>
    /// Moves the source file if the path has changed
    /// </summary>
    /// <param name="previousPath">Previous source path, moved from here if necessary</param>
    /// <param name="path">Latest source path, destination</param>
    /// <returns></returns>
    private static bool MoveIfNecessary(string previousPath, string path)
    {
        if (previousPath == path || string.IsNullOrWhiteSpace(previousPath))
            return false;

        Log.Debug($" Moving {previousPath} -> {path} ...");

        try
        {
            File.Move(previousPath, path, true);
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to move '{previousPath}' -> '{path}': {e}");
        }

        return false;
    }

    #region File path handling
    private string BuildFilepathFmt(Symbol symbol) => BuildFilepathFmt(symbol.Name, symbol.Namespace);
    

    private string BuildFilepathFmt(string name, string @namespace)
    {
        var dir = BuildAndCreateFolderFromNamespace(@namespace);
        return Path.Combine(dir, name + "{0}");

        string BuildAndCreateFolderFromNamespace(string symbolNamespace)
        {
            var rootNamespace = CsProjectFile.RootNamespace;
            symbolNamespace = symbolNamespace.StartsWith(rootNamespace) 
                                  ? symbolNamespace.Replace(rootNamespace, "")
                                  : symbolNamespace;
            
            if (string.IsNullOrWhiteSpace(symbolNamespace) || symbolNamespace == CsProjectFile.RootNamespace)
            {
                return Folder;
            }

            var namespaceParts = symbolNamespace.Split('.').Where(x => x.Length > 0);
            var subfolders = new [] { Folder }.Concat(namespaceParts).ToArray();
            var directory = Path.Combine(subfolders); 
            Directory.CreateDirectory(directory);
            return directory;
        }
    }
    #endregion

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

    public void LocateSourceCodeFiles()
    {
        _sourceCodeFiles.Clear();
        Directory.EnumerateFiles(Folder, $"*{SourceCodeExtension}", SearchOption.AllDirectories)
                 .AsParallel()
                 .ForAll(file =>
                         {
                             using var streamReader = new StreamReader(file);
                             while (!streamReader.EndOfStream)
                             {
                                 var line = streamReader.ReadLine();
                                 if (line == null)
                                     break;

                                 var firstQuoteIndex = line.IndexOf('"');
                                 if (firstQuoteIndex == -1)
                                     continue;

                                 var secondQuoteIndex = line.IndexOf('"', firstQuoteIndex + 1);

                                 if (secondQuoteIndex == -1)
                                     continue;

                                 var stringSpan = line.AsSpan(firstQuoteIndex + 1, secondQuoteIndex - firstQuoteIndex - 1);

                                 if (!Guid.TryParse(stringSpan, out var guid))
                                     continue;

                                 _sourceCodeFiles.TryAdd(guid, file);
                                 break;
                             }
                         });
    }

    public bool TryGetSourceCodePath(Symbol symbol, out string path) => _sourceCodeFiles.TryGetValue(symbol.Id, out path);

    public static bool IsSaving => Interlocked.Read(ref _savingCount) > 0;
    private static long _savingCount;
    static readonly FileStreamOptions SaveOptions = new() { Mode = FileMode.Create, Access = FileAccess.ReadWrite };
    private readonly ConcurrentDictionary<Guid, string> _sourceCodeFiles = new();

    private const string SourceCodeExtension = ".cs";

    private class EditablePackageFsWatcher : FileSystemWatcher
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