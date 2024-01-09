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
        WriteAllSymbolFilesOf(SymbolUis);
        UnmarkAsSaving();
    }

    /// <summary>
    /// Note: This does NOT clean up 
    /// </summary>
    internal void SaveModifiedSymbols()
    {
        MarkAsSaving();
        
        var modifiedSymbolUis = SymbolUis.Where(symbolUi => symbolUi.HasBeenModified).ToList();
        Log.Debug($"{CsProjectFile.Name}: Saving {modifiedSymbolUis.Count} modified symbols...");

        WriteAllSymbolFilesOf(modifiedSymbolUis);

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

    private void WriteSymbolUi(SymbolUi symbolUi, string filePathFmt)
    {
        var uiFilePath = string.Format(filePathFmt, '_' + symbolUi.Symbol.Id.ToString() + SymbolUiExtension);

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
        if (previousPath == path)
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
    private string BuildFilepathFmt(Symbol symbol)
    {
        var dir = BuildAndCreateFolderFromNamespace(Folder, symbol.Namespace);
        return Path.Combine(dir, symbol.Name + "{0}");

        string BuildAndCreateFolderFromNamespace(string rootFolder, string symbolNamespace)
        {
            if (string.IsNullOrEmpty(symbolNamespace) || symbolNamespace == AssemblyInformation.Name)
            {
                return rootFolder;
            }

            var namespaceParts = symbolNamespace.Split('.');
            var assemblyRootParts = Folder.Split(Path.DirectorySeparatorChar);
            var rootOfOperatorDirectoryIndex = Array.IndexOf(assemblyRootParts, AssemblyInformation.Name);
            var operatorRootParts = assemblyRootParts.AsSpan()[..(rootOfOperatorDirectoryIndex)].ToArray();

            var directory = Path.Combine(operatorRootParts.Concat(namespaceParts).ToArray());
            Directory.CreateDirectory(directory);
            return directory;
        }
    }
    #endregion

    private void MarkAsSaving()
    {
        Interlocked.Increment(ref _savingCount);
        _fileSystemWatcher.EnableRaisingEvents = false;
    }

    private void UnmarkAsSaving()
    {
        Interlocked.Decrement(ref _savingCount);
        _fileSystemWatcher.EnableRaisingEvents = true;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs args)
    {
        MarkAsModified();
    }

    private void OnFileRenamed(object sender, RenamedEventArgs args)
    {
        EditorUi.Instance.ShowMessageBox($"File {args.OldFullPath} renamed to {args.FullPath}. Please do not do this while the editor is running.");
        return;
        MarkAsModified();
        var oldPath = args.OldFullPath;
        var newPath = args.FullPath;

        //determine if path is a file or a directory
        FileAttributes attrs = File.GetAttributes(oldPath);
        if ((attrs & FileAttributes.Directory) == FileAttributes.Directory)
        {
            // update all files previously in directory
        }
        else
        {
            // update single file
        }
    }

    public void FindSourceCodeFiles()
    {
        var sourceCodeFiles = Directory.EnumerateFiles(Folder, $"*{SourceCodeExtension}", SearchOption.AllDirectories);
        sourceCodeFiles.AsParallel().ForAll(file =>
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

    public static bool IsSaving => Interlocked.Read(ref _savingCount) > 0;
    private static long _savingCount;
    static readonly FileStreamOptions SaveOptions = new() { Mode = FileMode.Create, Access = FileAccess.ReadWrite };
    private readonly ConcurrentDictionary<Guid, string> _sourceCodeFiles = new();

    public const string SourceCodeExtension = ".cs";
    public const string SymbolExtension = ".t3";
    public const string SymbolUiExtension = ".t3ui";

    class EditablePackageFsWatcher : FileSystemWatcher
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