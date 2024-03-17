#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Core.SystemUi;
using T3.Editor.Compilation;

namespace T3.Editor.UiModel;


internal sealed partial class EditableSymbolProject : EditorSymbolPackage
{
    public override AssemblyInformation AssemblyInformation => CsProjectFile.Assembly;

    static EditableSymbolProject()
    {
        ProjectAdded += (project, homeSymbol) =>
                        {
                            if (RootSymbolUi == null)
                            {
                                const string message = "Root symbol is null! It must be created before adding projects!";
                                throw new Exception(message);
                            }
                            
                            var opSize = SymbolChildUi.DefaultOpSize;
                            RootSymbolUi.AddChild(homeSymbol, 
                                                  addedChildId: Guid.NewGuid(), 
                                                  posInCanvas: new Vector2(0, _newProjectPosition), 
                                                  size: opSize, 
                                                  name: "");
                            
                            _newProjectPosition += (int)opSize.Y + 20;
                        };
    }

    internal static void EnableRootSaving(bool enabled)
    {
        if (RootSymbolUi == null)
        {
            throw new Exception("RootSymbolUi is null");
        }
        
        RootSymbolUi.ForceUnmodified = enabled;
    }

    /// <summary>
    /// Create a new <see cref="EditableSymbolProject"/> using the given <see cref="CsProjectFile"/>.
    /// This calls the base constructor with a null assembly as we override the assembly property
    /// </summary>
    /// <param name="csProjectFile"></param>
    public EditableSymbolProject(CsProjectFile csProjectFile) : base(assembly: null!)
    {
        CsProjectFile = csProjectFile;
        AllProjectsRw.Add(this);
        _csFileWatcher = new CodeFileWatcher(this, OnFileChanged, OnFileRenamed);
        SymbolAdded += OnSymbolAdded;
        SymbolUpdated += OnSymbolUpdated;
        SymbolRemoved += OnSymbolRemoved;
    }

    public bool TryCreateHome()
    {
        if (!CsProjectFile.Assembly.HasHome)
            return false;

        Log.Debug($"Creating home for {CsProjectFile.Name}...");
        var homeGuid = CsProjectFile.Assembly.HomeGuid;
        var homeSymbol = Symbols[homeGuid];

        ProjectAdded?.Invoke(this, homeSymbol);

        return true;
    }

    public void OpenProjectInCodeEditor()
    {
        CoreUi.Instance.OpenWithDefaultApplication(CsProjectFile.FullPath);
    }
    
    public bool TryOpenCSharpInEditor(Symbol symbol)
    {
        var guid = symbol.Id;
        if (!_filePathHandlers.TryGetValue(guid, out var filePathHandler))
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
        SymbolUiRegistry.EntriesEditable[symbol.Id] = symbolUi;
        SymbolUis[symbol.Id] = symbolUi;

        Log.Debug($"Replaced symbol ui for {symbol.Name}");
    }

    private void GiveSymbolToPackage(Guid id, EditableSymbolProject newDestinationProject)
    {
        Symbols.Remove(id, out var symbol);
        SymbolUis.Remove(id, out var symbolUi);
        _filePathHandlers.Remove(id, out var symbolPathHandler);
        
        Debug.Assert(symbol != null);
        Debug.Assert(symbolUi != null);
        Debug.Assert(symbolPathHandler != null);

        symbol.SymbolPackage = newDestinationProject;

        newDestinationProject.Symbols.Add(id, symbol);
        newDestinationProject.SymbolUis.TryAdd(id, symbolUi);

        newDestinationProject._filePathHandlers.TryAdd(id, symbolPathHandler);

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
        AllProjectsRw.Remove(this);
    }

    public readonly CsProjectFile CsProjectFile;
    private ResourceFileWatcher _resourceFileWatcher;
    public override ResourceFileWatcher FileWatcher => _resourceFileWatcher;

    private static readonly List<EditableSymbolProject> AllProjectsRw = [];
    public static readonly IReadOnlyList<EditableSymbolProject> AllProjects = AllProjectsRw;
    
    /// <summary>
    /// Event that is raised when a new project is added
    /// Symbol is the new project's <see href="T3.Core.Operator.Attributes.HomeAttribute"/> symbol
    /// </summary>
    public static event Action<EditableSymbolProject, Symbol>? ProjectAdded;

    public override bool IsModifiable => true;

    private static int _newProjectPosition = 0;
}