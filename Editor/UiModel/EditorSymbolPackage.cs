#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Core.Resource;
using T3.Core.Utils;
using T3.Editor.External;
using T3.Editor.Gui.ChildUi;

namespace T3.Editor.UiModel;

// todo - make abstract, create NugetSymbolPackage
internal class EditorSymbolPackage(AssemblyInformation assembly) : SymbolPackage(assembly)
{
    public void LoadUiFiles(IEnumerable<Symbol> newlyReadSymbols, out IReadOnlyCollection<SymbolUi> newlyReadSymbolUis,
                            out IReadOnlyCollection<SymbolUi> preExistingSymbolUis)
    {
        var newSymbols = newlyReadSymbols.ToDictionary(result => result.Id, symbol => symbol);
        var newSymbolsWithoutUis = new ConcurrentDictionary<Guid, Symbol>(newSymbols);
        preExistingSymbolUis = SymbolUis.Values.ToArray();
        Log.Debug($"{AssemblyInformation.Name}: Loading Symbol UIs from \"{Folder}\"");
        var newlyReadSymbolUiList = SymbolUiSearchFiles
                                             //.AsParallel()
                                             .Select(JsonFileResult<SymbolUi>.ReadAndCreate)
                                             .Where(result => newSymbols.ContainsKey(result.Guid))
                                             .Select(uiJson =>
                                                     {
                                                         if (!SymbolUiJson.TryReadSymbolUi(uiJson.JToken, uiJson.Guid, out var symbolUi))
                                                         {
                                                             Log.Error($"Error reading symbol Ui for {uiJson.Guid} from file \"{uiJson.FilePath}\"");
                                                             return null;
                                                         }

                                                         newSymbolsWithoutUis.Remove(symbolUi.Symbol.Id, out _);
                                                         var id = symbolUi.Symbol.Id;

                                                         var added = SymbolUis.TryAdd(id, symbolUi);
                                                         if (!added)
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

        foreach (var (guid, symbol) in newSymbolsWithoutUis)
        {
            var symbolUi = new SymbolUi(symbol, false);

            if (!SymbolUis.TryAdd(guid, symbolUi))
            {
                Log.Error($"{AssemblyInformation.Name}: Duplicate symbol UI for {symbol.Name}?");
                continue;
            }

            newlyReadSymbolUiList.Add(symbolUi);
            OnSymbolUiLoaded(null, symbolUi);
        }

        newlyReadSymbolUis = newlyReadSymbolUiList;
    }

    protected virtual void OnSymbolUiLoaded(string? path, SymbolUi symbolUi)
    {
        // do nothing
    }

    private static void RegisterCustomChildUi(Symbol symbol)
    {
        var valueInstanceType = symbol.InstanceType;
        if (typeof(IDescriptiveFilename).IsAssignableFrom(valueInstanceType))
        {
            CustomChildUiRegistry.Entries.TryAdd(valueInstanceType, DescriptiveUi.DrawChildUi);
        }
    }

    public void RegisterUiSymbols(bool enableLog, IEnumerable<SymbolUi> newSymbolUis, IEnumerable<SymbolUi> preExistingSymbolUis)
    {
        Log.Debug($@"{AssemblyInformation.Name}: Registering UI entries...");

        foreach (var symbolUi in preExistingSymbolUis)
        {
            symbolUi.UpdateConsistencyWithSymbol();
        }

        foreach (var symbolUi in newSymbolUis)
        {
            var symbol = symbolUi.Symbol;

            RegisterCustomChildUi(symbol);

            if (!SymbolUiRegistry.EntriesEditable.TryAdd(symbol.Id, symbolUi))
            {
                SymbolUis.Remove(symbol.Id, out _);
                Log.Error($"Can't load UI for [{symbolUi.Symbol.Name}] Registry already contains id {symbolUi.Symbol.Id}.");
                continue;
            }

            symbolUi.UpdateConsistencyWithSymbol();
            if (enableLog)
                Log.Debug($"Add UI for {symbolUi.Symbol.Name} {symbolUi.Symbol.Id}");
        }
    }

    public static void InitializeRoot(SymbolPackage package)
    {
        var rootInstanceId = new Guid("fa3db58b-068d-427d-96e7-8144f4721db3");
        var rootSymbolId = new Guid("341992ea-6343-4485-9fef-3a84bb36199d");
        
        if (!package.TryCreateInstance(rootSymbolId, rootInstanceId, null, out var rootInstance))
        {
            throw new Exception("Could not create root symbol instance.");
        }
        
        SymbolUiRegistry.Entries.TryGetValue(rootSymbolId, out RootSymbolUi);
        
        RootInstance = rootInstance;
    }
    
    public override void Dispose()
    {
        base.Dispose();
        
        var symbolUis = SymbolUis.Values.ToArray();

        foreach (var symbolUi in symbolUis)
        {
            SymbolUiRegistry.EntriesEditable.TryRemove(symbolUi.Symbol.Id, out _);
            SymbolUis.TryRemove(symbolUi.Symbol.Id, out _);
        }
        
        ShaderLinter.RemovePackage(this);
    }

    /// <summary>
    /// Looks for source codes in the project folder and subfolders and tries to find the symbol id in the source code
    /// </summary>
    public virtual void LocateSourceCodeFiles()
    {
        _sourceCodePaths ??= new ConcurrentDictionary<Guid, string>();

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
                    Log.Error($"{AssemblyInformation.Name}: Failed to parse guid from {guidSpan.ToString()} in \"{file}\"");
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

    public virtual bool TryGetSourceCodePath(Symbol symbol, out string? sourceCodePath)
    {
        return _sourceCodePaths!.TryGetValue(symbol.Id, out sourceCodePath);
    }

    protected virtual void OnSourceCodeLocated(string path, Guid guid)
    {
        _sourceCodePaths![guid] = path;
    }

    public static Instance? RootInstance { get; private set; }
    private protected static SymbolUi? RootSymbolUi;

    protected readonly ConcurrentDictionary<Guid, SymbolUi> SymbolUis = new();

    protected virtual IEnumerable<string> SymbolUiSearchFiles =>
        Directory.EnumerateFiles(Path.Combine(Folder, SymbolUiSubFolder), $"*{SymbolUiExtension}", SearchOption.AllDirectories);
    
    public override bool IsModifiable => false;
    private ConcurrentDictionary<Guid, string>? _sourceCodePaths;

    protected virtual IEnumerable<string> SourceCodeSearchFiles => Directory.EnumerateFiles(Path.Combine(Folder, SourceCodeSubFolder), $"*{SourceCodeExtension}", SearchOption.AllDirectories);
    
    internal const string SourceCodeExtension = ".cs";
    public const string SymbolUiExtension = ".t3ui";
    public const string SymbolUiSubFolder = "SymbolUis";
    public const string SourceCodeSubFolder = "SourceCode";

    public void InitializeShaderLinting(IReadOnlyList<IResourcePackage> sharedShaderPackages)
    {
        ShaderLinter.AddPackage(this, sharedShaderPackages);
    }
}