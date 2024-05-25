#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using T3.Core.Compilation;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Core.SystemUi;
using T3.Editor.Compilation;

namespace T3.Editor.UiModel;

[DebuggerDisplay("{DisplayName}")]
internal sealed partial class EditableSymbolProject : EditorSymbolPackage
{
    public override AssemblyInformation AssemblyInformation => CsProjectFile.Assembly;
    public override string DisplayName { get; }

    /// <summary>
    /// Create a new <see cref="EditableSymbolProject"/> using the given <see cref="CsProjectFile"/>.
    /// </summary>
    /// <param name="csProjectFile"></param>
    public EditableSymbolProject(CsProjectFile csProjectFile) : base(assembly: csProjectFile.Assembly)
    {
        CsProjectFile = csProjectFile;
        lock(_allProjects)
            _allProjects.Add(this);
        Log.Debug($"Added project {csProjectFile.Name}");
        _csFileWatcher = new CodeFileWatcher(this, OnFileChanged, OnFileRenamed);
        DisplayName = $"{csProjectFile.Name} ({CsProjectFile.RootNamespace})";
        SymbolUpdated += OnSymbolUpdated;
        SymbolRemoved += OnSymbolRemoved;
    }

    public void OpenProjectInCodeEditor()
    {
        CoreUi.Instance.OpenWithDefaultApplication(CsProjectFile.FullPath);
    }
    
    public bool TryOpenCSharpInEditor(Symbol symbol)
    {
        var guid = symbol.Id;
        if (!FilePathHandlers.TryGetValue(guid, out var filePathHandler))
        {
            Log.Error($"No file path handler found for symbol {guid}");
            return false;
        }

        var sourceCodePath = filePathHandler.SourceCodePath;
        if (string.IsNullOrWhiteSpace(sourceCodePath))
        {
            Log.Error($"No source code path found for symbol {guid}");
            return false;
        }
        
        OpenProjectInCodeEditor();
        CoreUi.Instance.OpenWithDefaultApplication(sourceCodePath);
        return true;
    }

    public void ReplaceSymbolUi(SymbolUi symbolUi)
    {
        var symbol = symbolUi.Symbol;
        SymbolUiDict[symbol.Id] = symbolUi;
        symbolUi.FlagAsModified();
        symbolUi.ReadOnly = false;
        SaveSymbolFile(symbolUi);

        Log.Debug($"Replaced symbol ui for {symbol.Name}");
    }

    private void GiveSymbolToPackage(Guid id, EditableSymbolProject newDestinationProject)
    {
        SymbolDict.Remove(id, out var symbol);
        SymbolUiDict.Remove(id, out var symbolUi);
        FilePathHandlers.Remove(id, out var symbolPathHandler);
        
        Debug.Assert(symbol != null);
        Debug.Assert(symbolUi != null);
        Debug.Assert(symbolPathHandler != null);

        symbol.SymbolPackage = newDestinationProject;

        newDestinationProject.SymbolDict.TryAdd(id, symbol);
        newDestinationProject.SymbolUiDict.TryAdd(id, symbolUi);

        newDestinationProject.FilePathHandlers.TryAdd(id, symbolPathHandler);

        symbolUi.FlagAsModified();
        newDestinationProject.MarkAsNeedingRecompilation();
        MarkAsNeedingRecompilation();
    }


    public override string Folder => CsProjectFile.Directory;

    private string ExcludeFolder => Path.Combine(Folder, "bin");

    protected override IEnumerable<string> SymbolUiSearchFiles => FindFilesOfType(SymbolUiExtension);

    protected override IEnumerable<string> SymbolSearchFiles => FindFilesOfType(SymbolExtension);
    
    protected override IEnumerable<string> SourceCodeSearchFiles => FindFilesOfType(SourceCodeExtension);

    private IEnumerable<string> FindFilesOfType(string fileExtension)
    {
        return Directory.EnumerateDirectories(Folder)
                        .Where(x => !x.StartsWith(ExcludeFolder))
                        .SelectMany(x => Directory.EnumerateFiles(x, $"*{fileExtension}", SearchOption.AllDirectories))
                        .Concat(Directory.EnumerateFiles(Folder, $"*{fileExtension}"));
    }

    public override void InitializeResources()
    {
        base.InitializeResources();
        _resourceFileWatcher = new ResourceFileWatcher(ResourcesFolder);
    }

    public override void Dispose()
    {
        base.Dispose();
        FileWatcher.Dispose();

        lock (_allProjects)
        {
            var currentProjects = _allProjects.ToList();
            currentProjects.Remove(this);
            _allProjects = new ConcurrentBag<EditableSymbolProject>(currentProjects);
        }
    }

    public readonly CsProjectFile CsProjectFile;
    private ResourceFileWatcher _resourceFileWatcher;
    public override ResourceFileWatcher FileWatcher => _resourceFileWatcher;
    public override bool IsReadOnly => false;

    private static ConcurrentBag<EditableSymbolProject> _allProjects = [];
    public static readonly IEnumerable<EditableSymbolProject> AllProjects = _allProjects;
}