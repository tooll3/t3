#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using T3.Core.Compilation;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Core.Utils;
using T3.Editor.External;
using T3.Editor.Gui.ChildUi;

namespace T3.Editor.UiModel;

// todo - make abstract, create NugetSymbolPackage
internal class EditorSymbolPackage : SymbolPackage
{
    public EditorSymbolPackage(AssemblyInformation assembly) : base(assembly)
    {
        Log.Debug($"Added package {assembly.Name}");
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
            Log.Error($"No file path handler found for {guid}");
        }
    }

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

    private static void UnregisterCustomChildUi(Symbol symbol)
    {
        CustomChildUiRegistry.EntriesRw.Remove(symbol.InstanceType, out _);
    }

    public void RegisterUiSymbols(bool parallel, SymbolUi[] newSymbolUis, SymbolUi[] preExistingSymbolUis)
    {
        Log.Debug($@"{DisplayName}: Registering UI entries...");

        
        foreach (var symbolUi in preExistingSymbolUis)
        {
            symbolUi.UpdateConsistencyWithSymbol();
        }

        var descriptiveDrawFunc = DescriptiveUi.DrawChildUiDelegate;

        if (parallel)
        {
            newSymbolUis
               .AsParallel()
               .ForAll(RegisterSymbolUi);
        }
        else
        {
            foreach (var symbolUi in newSymbolUis)
            {
                RegisterSymbolUi(symbolUi);
            }
        }

        return;

        void RegisterSymbolUi(SymbolUi symbolUi)
        {
            var symbol = symbolUi.Symbol;
            var customUiEntries = CustomChildUiRegistry.EntriesRw;
            var operatorInfo = AssemblyInformation.OperatorTypeInfo[symbol.Id];
            
            if (operatorInfo.IsDescriptiveFileNameType)
            {
                customUiEntries.TryAdd(symbol.InstanceType, descriptiveDrawFunc);
            }
            
            symbolUi.UpdateConsistencyWithSymbol();
            //Log.Debug($"Add UI for {symbolUi.Symbol.Name} {symbolUi.Symbol.Id}");
        }
    }
    
    public override void Dispose()
    {
        base.Dispose();
        ClearSymbolUis();
        ShaderLinter.RemovePackage(this);
    }

    private void ClearSymbolUis()
    {
        var symbolUis = SymbolUiDict.Values.ToArray();

        foreach (var symbolUi in symbolUis)
        {
            var symbol = symbolUi.Symbol;
            SymbolUiDict.TryRemove(symbol.Id, out _);
            UnregisterCustomChildUi(symbol);
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
            Log.Debug($"{AssemblyInformation.Name}: Found {sourceCodeCount} operator source code files out of {sourceCodeAttempts} files.");
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

    private Instance? _rootInstance;

    public bool TryGetRootInstance(out Instance? rootInstance)
    {
        if (!HasHome)
        {
            rootInstance = null;
            return false;
        }

        if (_rootInstance != null)
        {
            rootInstance = _rootInstance;
            return true;
        }

        var rootSymboLUi = SymbolUiDict[AssemblyInformation.HomeGuid];
        Log.Debug($"{DisplayName}: Found home symbol");

        var symbol = rootSymboLUi.Symbol;
        if (!symbol.TryCreateParentlessInstance(out rootInstance))
        {
            Log.Error($"Failed to create home instance for {AssemblyInformation.Name}'s symbol {symbol.Name} with id {symbol.Id}");
            return false;
        }
        
        _rootInstance = rootInstance;
        return true;
    }

    internal bool HasHome => AssemblyInformation.HasHome;
    
    
    protected readonly ConcurrentDictionary<Guid, SymbolUi> SymbolUiDict = new();
    public IReadOnlyDictionary<Guid, SymbolUi> SymbolUis => SymbolUiDict;

    protected virtual IEnumerable<string> SymbolUiSearchFiles =>
        Directory.EnumerateFiles(Path.Combine(Folder, SymbolUiSubFolder), $"*{SymbolUiExtension}", SearchOption.AllDirectories);
    
    protected virtual IEnumerable<string> SourceCodeSearchFiles => Directory.EnumerateFiles(Path.Combine(Folder, SourceCodeSubFolder), $"*{SourceCodeExtension}", SearchOption.AllDirectories);
    private readonly ConcurrentDictionary<Guid, SymbolPathHandler> _filePathHandlers = new();
    protected IDictionary<Guid, SymbolPathHandler> FilePathHandlers => _filePathHandlers;
    
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
            Log.Error($"Failed to reload symbol for symbol {id}");
            return;
        }
        
        // transfer instances over to the new symbol and update them
        symbol.ReplaceWith(newSymbol);
        UpdateSymbolInstances(symbol);
        
        var symbolUiJson = JsonFileResult<SymbolUi>.ReadAndCreate(symbolUiPath);

        if (!SymbolUiJson.TryReadSymbolUi(symbolUiJson.JToken, symbol, out var newSymbolUi))
        {
            throw new Exception($"Failed to reload symbol ui for symbol {id}");
        }

        // override registry values
        newSymbolUi.UpdateConsistencyWithSymbol();
        symbolUi.ReplaceWith(newSymbolUi);
    }

    public bool TryGetSymbolUi(Guid rSymbolId, [NotNullWhen(true)] out SymbolUi? symbolUi)
    {
        return SymbolUiDict.TryGetValue(rSymbolId, out symbolUi);
    }
}