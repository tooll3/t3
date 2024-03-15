using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Editor.Gui.ChildUi;

namespace T3.Editor.UiModel;

// todo - make abstract, create NugetSymbolPackage
internal abstract class EditorSymbolPackage : StaticSymbolPackage
{
    internal EditorSymbolPackage(AssemblyInformation assembly, bool initializeFileWatcher) : base(assembly, false)
    {
        if (initializeFileWatcher)
        {
            InitializeFileWatcher();
        }
    }

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
    
    protected abstract void OnSymbolUiLoaded(string? path, SymbolUi symbolUi);

    private static void RegisterCustomChildUi(Symbol symbol)
    {
        var valueInstanceType = symbol.InstanceType;
        if (typeof(IDescriptiveFilename).IsAssignableFrom(valueInstanceType))
        {
            CustomChildUiRegistry.Entries.TryAdd(valueInstanceType, DescriptiveUi.DrawChildUi);
        }
    }

    protected override void OnSymbolRemoved(Symbol symbol)
    {
        var id = symbol.Id;
        SymbolUis.Remove(id, out _);
        SymbolUiRegistry.EntriesEditable.Remove(id, out _);
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

    protected override bool RemoveSymbol(Guid guid)
    {
        return base.RemoveSymbol(guid)
               && SymbolUis.Remove(guid, out _)
               && SymbolUiRegistry.EntriesEditable.Remove(guid, out _);
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
        
        EnableDisableRootSaving(false);

        RootInstance = rootInstance;
    }

    internal static void EnableDisableRootSaving(bool enabled)
    {
        if (enabled)
        {
            RootSymbolUi.DisableForceUnmodified();
        }
        else
        {
            RootSymbolUi.ForceUnmodified();
        }
    }

    public static Instance RootInstance { get; private set; }
    private protected static SymbolUi RootSymbolUi;

    protected readonly ConcurrentDictionary<Guid, SymbolUi> SymbolUis = new();
    protected override string ResourcesSubfolder => "Resources";

    protected virtual IEnumerable<string> SymbolUiSearchFiles => Directory.EnumerateFiles(Path.Combine(Folder, "SymbolUis"), $"*{SymbolUiExtension}", SearchOption.AllDirectories);
    public override bool IsModifiable => false;
    internal const string SymbolUiExtension = ".t3ui";
}