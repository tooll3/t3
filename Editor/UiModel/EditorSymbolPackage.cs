#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using T3.Core.Compilation;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Core.SystemUi;
using T3.Core.Utils;
using T3.Editor.Compilation;
using T3.Editor.External;
using T3.Editor.Gui.ChildUi;

namespace T3.Editor.UiModel;

// todo - make abstract, create NugetSymbolPackage
/// <summary>
/// EditorSymbolPackage is a readonly <see cref="SymbolPackage"/>  with  <see cref="SymbolUi"/>s.
/// </summary>
/// <remarks>
/// Is used to created an <see cref="EditableSymbolProject"/>.
/// </remarks>
internal class EditorSymbolPackage : SymbolPackage
{
    /// <summary>
    /// Constructor for a successfully compiled package
    /// </summary>
    /// <param name="assembly">Main assembly of the package</param>
    public EditorSymbolPackage(AssemblyInformation assembly) : base(assembly)
    {
        Log.Debug($"Added package {assembly.Name}");
        SymbolAdded += OnSymbolAdded;
    }

    protected EditorSymbolPackage(AssemblyInformation assembly, string directory) : base(assembly, directory)
    {
        Log.Debug($"Added package {assembly.Name} in directory {directory}");
        SymbolAdded += OnSymbolAdded;
    }

    protected virtual void OnSymbolAdded(string? path, Symbol symbol)
    {
        var id = symbol.Id;
        if (_filePathHandlers.TryGetValue(id, out var handler))
            return;

        handler = new SymbolPathHandler(symbol, path);
        _filePathHandlers[id] = handler;
    }

    protected virtual void OnSymbolUiLoaded(string? path, SymbolUi symbolUi)
    {
        var id = symbolUi.Symbol.Id;
        _filePathHandlers[id].UiFilePath = path;
    }

    private void OnSourceCodeLocated(string path, Guid guid)
    {
        if (_filePathHandlers.TryGetValue(guid, out var handler))
        {
            handler.SourceCodePath = path;
        }
        else
        {
            Log.Error($"No file path handler found for {guid} / '{path}'");
        }
    }

    /// <summary>
    /// Loads UI files for the provided symbols. 
    /// This method does not delete UIs that have since been deleted
    /// </summary>
    /// <param name="parallel">If true, load and process in parallel</param>
    /// <param name="newlyReadSymbols">The symbols for which UI files should be loaded - just the new ones</param>
    /// <param name="newlyReadSymbolUis">Brand spankin new symbol UIs</param>
    /// <param name="preExistingSymbolUis">Symbol UIs that already existed at runtime - if this is the first time this package was loaded, this will be empty</param>
    public void LoadUiFiles(bool parallel, List<Symbol> newlyReadSymbols, out SymbolUi[] newlyReadSymbolUis,
                            out SymbolUi[] preExistingSymbolUis)
    {
        var newSymbols = newlyReadSymbols.ToDictionary(result => result.Id, symbol => symbol);
        var newSymbolsWithoutUis = new ConcurrentDictionary<Guid, Symbol>(newSymbols);
        preExistingSymbolUis = SymbolUiDict.Values.ToArray();
        Log.Debug($"{AssemblyInformation.Name}: Loading Symbol UIs from \"{Folder}\"");

        var enumerator = parallel ? SymbolUiSearchFiles.AsParallel() : SymbolUiSearchFiles;
        var newlyReadSymbolUiList = enumerator
                                   .Select(JsonFileResult<SymbolUi>.ReadAndCreate)
                                   .Where(result => newSymbols.ContainsKey(result.Guid))
                                   .Select(uiJson =>
                                           {
                                               if (!SymbolUiJson.TryReadSymbolUi(uiJson.JToken, newSymbols[uiJson.Guid], out var symbolUi))
                                               {
                                                   Log.Error($"Error reading symbol Ui for {uiJson.Guid} from file \"{uiJson.FilePath}\"");
                                                   return null;
                                               }

                                               newSymbolsWithoutUis.Remove(symbolUi.Symbol.Id, out _);
                                               var id = symbolUi.Symbol.Id;

                                               if (!SymbolUiDict.TryAdd(id, symbolUi))
                                               {
                                                   Log.Error($"{AssemblyInformation.Name}: Duplicate symbol UI for {symbolUi.Symbol.Name}?");
                                                   return null;
                                               }

                                               OnSymbolUiLoaded(uiJson.FilePath, symbolUi);
                                               return symbolUi;
                                           })
                                   .Where(symbolUi => symbolUi != null)
                                   .Select(symbolUi => symbolUi!)
                                   .ToList();

        if (newSymbolsWithoutUis.Count == 0)
        {
            newlyReadSymbolUis = newlyReadSymbolUiList.ToArray();
            return;
        }

        foreach (var (guid, symbol) in newSymbolsWithoutUis)
        {
            var symbolUi = new SymbolUi(symbol, false);

            if (!SymbolUiDict.TryAdd(guid, symbolUi))
            {
                Log.Error($"{AssemblyInformation.Name}: Duplicate symbol UI for {symbol.Name}?");
                continue;
            }

            newlyReadSymbolUiList.Add(symbolUi);
            OnSymbolUiLoaded(null, symbolUi);
        }

        newlyReadSymbolUis = newlyReadSymbolUiList.ToArray();
    }

    public void RegisterUiSymbols(SymbolUi[] newSymbolUis, SymbolUi[] preExistingSymbolUis)
    {
        Log.Debug($@"{DisplayName}: Registering UI entries...");

        foreach (var symbolUi in preExistingSymbolUis)
        {
            RegisterSymbolUi(symbolUi);
        }

        foreach (var symbolUi in newSymbolUis)
        {
            RegisterSymbolUi(symbolUi);
        }

        return;

        void RegisterSymbolUi(SymbolUi symbolUi)
        {
            var symbol = symbolUi.Symbol;
            var operatorInfo = AssemblyInformation.OperatorTypeInfo[symbol.Id];

            // register descriptive UI
            if (operatorInfo.IsDescriptiveFileNameType)
            {
                CustomChildUiRegistry.Register(symbol.InstanceType, DescriptiveUi.DrawChildUiDelegate, _descriptiveUiTypes);
            }

            symbolUi.UpdateConsistencyWithSymbol();
            symbolUi.ClearModifiedFlag();
            //Log.Debug($"Add UI for {symbolUi.Symbol.Name} {symbolUi.Symbol.Id}");
        }
    }

    public override void Dispose()
    {
        ClearSymbolUis();
        base.Dispose();
        UnregisterDescriptiveUis();
        ShaderLinter.RemovePackage(this);
    }

    private void ClearSymbolUis()
    {
        var symbolUis = SymbolUiDict.Values.ToArray();

        foreach (var symbolUi in symbolUis)
        {
            try
            {
                var symbol = symbolUi.Symbol;
                SymbolUiDict.TryRemove(symbol.Id, out _);
                CustomChildUiRegistry.Remove(symbol.InstanceType);
            }
            catch (KeyNotFoundException)
            {
                Log.Warning("Can't remove obsolete symbol.");
            }
        }
    }

    /// <summary>
    /// Looks for source codes in the project folder and subfolders and tries to find the symbol id in the source code
    /// </summary>
    public virtual void LocateSourceCodeFiles()
    {
        #if DEBUG
        int sourceCodeCount = 0;
        int sourceCodeAttempts = 0;
        #endif

        SourceCodeSearchFiles
           .AsParallel()
           .ForAll(ParseCodeFile);

        #if DEBUG
        if (sourceCodeCount == 0 && sourceCodeAttempts != 0)
        {
            Log.Error($"{AssemblyInformation.Name}: No source code files found in project folder.");
        }
        else
        {
            Log.Debug($"{AssemblyInformation.Name}: Found {sourceCodeCount} operator source code files out of {sourceCodeAttempts} C# files.");
        }
        #endif

        return;

        void ParseCodeFile(string file)
        {
            #if DEBUG
            Interlocked.Increment(ref sourceCodeAttempts);
            #endif

            var streamReader = new StreamReader(file);

            var guid = Guid.Empty;
            while (!streamReader.EndOfStream)
            {
                var line = streamReader.ReadLine();
                if (line == null)
                    break;

                if (!StringUtils.TryFindIgnoringAllWhitespace(line, "[Guid(\"", StringUtils.SearchResultIndex.AfterTerm, out var guidStartIndex))
                    continue;

                var indexOfQuote = line.IndexOf('"', guidStartIndex);
                var guidSpan = line.AsSpan(guidStartIndex, indexOfQuote - guidStartIndex);

                if (!Guid.TryParse(guidSpan, out guid))
                {
                    Log.Error($"{DisplayName}: Failed to parse guid from {guidSpan.ToString()} in \"{file}\"");
                    continue;
                }

                break;
            }

            streamReader.Close();
            streamReader.Dispose();

            if (guid == Guid.Empty)
                return;

            #if DEBUG
            Interlocked.Increment(ref sourceCodeCount);
            #endif

            OnSourceCodeLocated(file, guid);
        }
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

    public void InitializeShaderLinting(IReadOnlyList<IResourcePackage> sharedShaderPackages)
    {
        ShaderLinter.AddPackage(this, sharedShaderPackages);
    }

    internal bool HasHome
    {
        get
        {
            var releaseInfo = ReleaseInfo;
            return releaseInfo.HomeGuid != Guid.Empty && Symbols.ContainsKey(releaseInfo.HomeGuid);
        }
    }

    protected readonly ConcurrentDictionary<Guid, SymbolUi> SymbolUiDict = new();
    public IReadOnlyDictionary<Guid, SymbolUi> SymbolUis => SymbolUiDict;

    protected virtual IEnumerable<string> SymbolUiSearchFiles =>
        Directory.EnumerateFiles(Path.Combine(Folder, SymbolUiSubFolder), $"*{SymbolUiExtension}", SearchOption.AllDirectories);

    protected virtual IEnumerable<string> SourceCodeSearchFiles =>
        Directory.EnumerateFiles(Path.Combine(Folder, SourceCodeSubFolder), $"*{SourceCodeExtension}", SearchOption.AllDirectories);

    private readonly ConcurrentDictionary<Guid, SymbolPathHandler> _filePathHandlers = new();
    protected IDictionary<Guid, SymbolPathHandler> FilePathHandlers => _filePathHandlers;
    public Guid HomeSymbolId => ReleaseInfo.HomeGuid;

    internal const string SourceCodeExtension = ".cs";
    public const string SymbolUiExtension = ".t3ui";
    public const string SymbolUiSubFolder = "SymbolUis";
    public const string SourceCodeSubFolder = "SourceCode";

    public static IEnumerable<Symbol> AllSymbols => AllPackages
                                                   .Cast<EditorSymbolPackage>()
                                                   .Select(x => x.SymbolDict)
                                                   .SelectMany(x => x.Values);

    public static IEnumerable<SymbolUi> AllSymbolUis => AllPackages
                                                       .Cast<EditorSymbolPackage>()
                                                       .Select(x => x.SymbolUiDict)
                                                       .SelectMany(x => x.Values);

    public void Reload(SymbolUi symbolUi)
    {
        var symbol = symbolUi.Symbol;
        var id = symbol.Id;

        if (!_filePathHandlers.TryGetValue(id, out var pathHandler))
        {
            throw new Exception($"No path handler found for symbol {id}");
        }

        var symbolPath = pathHandler.SymbolFilePath;
        if (symbolPath == null)
        {
            throw new Exception($"No symbol path found for symbol {id}");
        }

        var symbolUiPath = pathHandler.UiFilePath;
        if (symbolUiPath == null)
        {
            throw new Exception($"No symbol ui path found for symbol {id}");
        }

        // reload single ui
        var symbolJson = JsonFileResult<Symbol>.ReadAndCreate(symbolPath);
        var result = SymbolJson.ReadSymbolRoot(symbol.Id, symbolJson.JToken, symbol.InstanceType, this);
        var newSymbol = result.Symbol;

        if (!TryReadAndApplyChildren(result))
        {
            symbol.ReplaceWithContentsOf(symbol); // ugly but necessary to make sure the right symbol is in the registry
            Log.Error($"Failed to reload symbol for symbol {id}");
            return;
        }

        // transfer instances over to the new symbol and update them
        symbol.ReplaceWithContentsOf(newSymbol);

        UpdateSymbolInstances(symbol, forceTypeUpdate: true);

        var symbolUiJson = JsonFileResult<SymbolUi>.ReadAndCreate(symbolUiPath);

        if (!SymbolUiJson.TryReadSymbolUi(symbolUiJson.JToken, symbol, out var newSymbolUi))
        {
            throw new Exception($"Failed to reload symbol ui for symbol {id}");
        }

        // override registry values
        newSymbolUi.UpdateConsistencyWithSymbol();
        symbolUi.ReplaceWith(newSymbolUi);
        symbolUi.ClearModifiedFlag();
    }

    public bool TryGetSymbolUi(Guid rSymbolId, [NotNullWhen(true)] out SymbolUi? symbolUi)
    {
        return SymbolUiDict.TryGetValue(rSymbolId, out symbolUi);
    }

    // todo - output should be an IDisposable wrapper and RemoveSymbolUi should be called in Dispose and made private
    internal bool TryCreateNewSymbol<T>([NotNullWhen(true)] out SymbolUi? symbolUi)
    {
        var containerOp = CreateSymbol(typeof(T), Guid.NewGuid());

        if (!SymbolDict.TryAdd(containerOp.Id, containerOp))
        {
            Log.Error($"Failed to add new symbol for {containerOp.Name} ({containerOp.Id})");
            symbolUi = null;
            return false;
        }

        symbolUi = new SymbolUi(containerOp, true);
        if (SymbolUiDict.TryAdd(containerOp.Id, symbolUi))
            return true;

        Log.Error($"Failed to add new symbol ui for {containerOp.Name} ({containerOp.Id})");
        return false;
    }

    internal bool RemoveSymbolUi(SymbolUi newContainerUi)
    {
        var symbolId = newContainerUi.Symbol.Id;
        return SymbolUiDict.TryRemove(symbolId, out _) && SymbolDict.TryRemove(symbolId, out _);
    }

    protected void UnregisterDescriptiveUis()
    {
        for (var index = _descriptiveUiTypes.Count - 1; index >= 0; index--)
        {
            var type = _descriptiveUiTypes[index];
            CustomChildUiRegistry.Remove(type);
            _descriptiveUiTypes.RemoveAt(index);
        }
    }

    public void InitializeCustomUis()
    {
        AssemblyInformation.Unloaded += UnloadCustomUis;
        AssemblyInformation.Loaded += LoadCustomUis;
    }

    private void LoadCustomUis(AssemblyInformation assemblyInformation)
    {
        var types = assemblyInformation.TypesInheritingFrom(typeof(IEditorUiExtension));

        foreach (var type in types)
        {
            var activated = assemblyInformation.CreateInstance(type);
            if (activated == null)
            {
                Log.Error($"Created null object for {type.Name}");
                continue;
            }

            var extension = (IEditorUiExtension)activated;
            try
            {
                extension.Initialize();
            }
            catch (Exception e)
            {
                Log.Error($"Failed to initialize UI extension {type.Name}: {e}");
                continue;
            }

            Log.Info($"Loaded UI initializer for {assemblyInformation.Name}: {type.Name}");
            _extensions.Add(extension);
        }

        if (_extensions.Count != 0 && assemblyInformation.OperatorTypeInfo.Count > 0)
        {
            BlockingWindow.Instance.ShowMessageBox("Custom UI extensions are not supported in projects that also have symbols defined. " +
                                                   "This may cause issues with exporting. It is recommended to start a new C# project for custom UIs.",
                                                   "Warning");
        }
    }

    private void UnloadCustomUis(AssemblyInformation _)
    {
        foreach (var extension in _extensions)
        {
            try
            {
                extension.Uninitialize();
            }
            catch (Exception e)
            {
                Log.Error($"Failed to uninitialize UI extension {extension.GetType().Name}: {e}");
            }
        }

        _extensions.Clear();
    }

    private readonly List<Type> _descriptiveUiTypes = [];
    private readonly List<IEditorUiExtension> _extensions = [];
}