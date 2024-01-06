using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Editor.Compilation;
using T3.Editor.Gui.ChildUi;
using T3.Editor.Gui.Windows;

// ReSharper disable RedundantNameQualifier

namespace T3.Editor.UiModel;

internal sealed partial class EditableSymbolData : EditorSymbolData
{
    public EditableSymbolData(CsProjectFile csProjectFile)
        : base(csProjectFile.Assembly)
    {
        CsProjectFile = csProjectFile;
        SymbolDataByProject.Add(csProjectFile, this);
    }

    public void RegisterUiSymbols(bool enableLog)
    {
        Log.Debug($@"{AssemblyInformation.Name}: Registering UI entries...");

        var dictionary = SymbolUiRegistry.Entries;

        foreach (var symbol in Symbols.Values)
        {
            if (!SymbolOwnersEditable.TryAdd(symbol.Id, this))
            {
                Log.Error($"Duplicate symbol id {symbol.Id}");
            }
        }

        foreach (var symbolUi in SymbolUis)
        {
            var symbol = symbolUi.Symbol;

            RegisterCustomChildUi(symbol);

            var added = dictionary.TryAdd(symbol.Id, symbolUi);

            if (!added)
            {
                Log.Error($"Can't load UI for [{symbolUi.Symbol.Name}] Registry already contains id {symbolUi.Symbol.Id}.");
                continue;
            }

            symbolUi.UpdateConsistencyWithSymbol();
            if (enableLog)
                Log.Debug($"Add UI for {symbolUi.Symbol.Name} {symbolUi.Symbol.Id}");
        }
    }

    // todo - "home" should be marked by an attribute rather than a hard-coded id
    public static bool TryCreateHome()
    {
        var gotHome = SymbolOwners.TryGetValue(HomeSymbolId, out var homeOwner);
        if (!gotHome)
        {
            return false;
        }

        var symbolUis = homeOwner.SymbolUis;

        var symbolUisById = symbolUis.ToDictionary(x => x.Symbol.Id, x => x);
        bool containsHome = symbolUisById.ContainsKey(HomeSymbolId);
        if (!containsHome)
        {
            Log.Error($"Could not find home symbol for {homeOwner.AssemblyInformation.Name} - something is wrong.");
            return false;
        }

        // Create instance of project op, all children are created automatically
        if (RootInstance != null)
        {
            throw new Exception("RootInstance already exists");
        }

        Console.WriteLine(@"Creating home...");
        var homeSymbol = symbolUisById[HomeSymbolId].Symbol;
        RootInstance = homeSymbol.CreateInstance(HomeInstanceId);
        ActiveProject = homeOwner;
        return true;
    }

    public override void SaveAll()
    {
        Log.Debug($"{AssemblyInformation.Name}: Saving...");

        MarkAsSaving();

        // Save all t3 and source files
        base.SaveAll();

        // Remove all old ui files before storing to get rid off invalid ones
        // TODO: this also seems dangerous, similar to how the Symbol SaveAll works
        var symbolUiFiles = Directory.GetFiles(Folder, $"*{SymbolUiExtension}", SearchOption.AllDirectories);
        foreach (var filepath in symbolUiFiles)
        {
            try
            {
                File.Delete(filepath);
            }
            catch (Exception e)
            {
                Log.Warning("Failed to deleted file '" + filepath + "': " + e);
            }
        }

        WriteSymbolUis(SymbolUis);

        UnmarkAsSaving();
    }

    /// <summary>
    /// Note: This does NOT clean up 
    /// </summary>
    internal void SaveModifiedSymbols()
    {
        MarkAsSaving();
        try
        {
            var modifiedSymbolUis = SymbolUis.Where(symbolUi => symbolUi.HasBeenModified).ToList();
            Log.Debug($"Saving {modifiedSymbolUis.Count} modified symbols...");

            var modifiedSymbols = modifiedSymbolUis.Select(symbolUi => symbolUi.Symbol).ToList();
            SaveSymbolDefinitionAndSourceFiles(modifiedSymbols);
            WriteSymbolUis(modifiedSymbolUis);
        }
        catch (System.InvalidOperationException e)
        {
            Log.Warning($"Saving failed. Please try to save manually ({e.Message})");
        }

        UnmarkAsSaving();
    }

    private void WriteSymbolUis(IEnumerable<SymbolUi> symbolUis)
    {
        var resourceManager = (EditorResourceManager)ResourceManager.Instance();

        foreach (var symbolUi in symbolUis)
        {
            var symbol = symbolUi.Symbol;
            var symbolFilePath = BuildFilepathForSymbol(symbol, SymbolUiExtension);

            using (var sw = new StreamWriter(symbolFilePath))
            using (var writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;
                SymbolUiJson.WriteSymbolUi(symbolUi, writer);
            }

            resourceManager.TrackOperatorFile(symbolFilePath, symbolUi.Symbol, CsProjectFile, OperatorUpdating.ResourceUpdateHandler);

            var symbolSourceFilepath = BuildFilepathForSymbol(symbol, SymbolData.SourceCodeExtension);
            resourceManager.TrackOperatorFile(symbolSourceFilepath, symbolUi.Symbol, CsProjectFile, OperatorUpdating.ResourceUpdateHandler);

            symbolUi.ClearModifiedFlag();
        }
    }

    public void UpdateUiEntriesForSymbol(Symbol symbol, SymbolUi symbolUi = null)
    {
        if (SymbolUiRegistry.Entries.TryGetValue(symbol.Id, out var foundSymbolUi))
        {
            foundSymbolUi.UpdateConsistencyWithSymbol();

            if (symbolUi != null)
            {
                Log.Warning("Symbol UI for symbol " + symbol.Id + " already exists. Disregarding new UI.");
            }
        }
        else
        {
            symbolUi ??= new SymbolUi(symbol);
            SymbolUiRegistry.Entries.Add(symbol.Id, symbolUi);
            SymbolUis.Add(symbolUi);
        }
    }

    public override void AddSymbol(Symbol newSymbol)
    {
        base.AddSymbol(newSymbol);
        var added = SymbolOwnersEditable.TryAdd(newSymbol.Id, this);
        if (!added)
            throw new Exception($"Symbol {newSymbol.Id} already exists in {AssemblyInformation.Name}");
        UpdateUiEntriesForSymbol(newSymbol);
        RegisterCustomChildUi(newSymbol);
    }

    public void AddSymbol(Symbol newSymbol, SymbolUi symbolUi)
    {
        base.AddSymbol(newSymbol);
        UpdateUiEntriesForSymbol(newSymbol, symbolUi);
        RegisterCustomChildUi(newSymbol);
    }

    internal static readonly Guid HomeSymbolId = Guid.Parse("dab61a12-9996-401e-9aa6-328dd6292beb");
    private static readonly Guid HomeInstanceId = Guid.Parse("12d48d5a-b8f4-4e08-8d79-4438328662f0");

    public override string Folder => CsProjectFile.Directory;

    public readonly CsProjectFile CsProjectFile;

    public static Instance RootInstance { get; private set; }

    public static IReadOnlyDictionary<CsProjectFile, EditableSymbolData> SymbolDataByAssembly => SymbolDataByProject;
    private static readonly Dictionary<CsProjectFile, EditableSymbolData> SymbolDataByProject = new();

    private static readonly ConcurrentDictionary<Guid, EditableSymbolData> SymbolOwnersEditable = new();
    public static IReadOnlyDictionary<Guid, EditableSymbolData> SymbolOwners => SymbolOwnersEditable;

    public static EditableSymbolData ActiveProject { get; private set; }

    public void RenameNameSpace(NamespaceTreeNode node, string nameSpace)
    {
        var orgNameSpace = node.GetAsString();
        foreach (var symbol in SymbolRegistry.Entries.Values)
        {
            if (!symbol.Namespace.StartsWith(orgNameSpace))
                continue;

            //var newNameSpace = parent + "."
            var newNameSpace = Regex.Replace(symbol.Namespace, orgNameSpace, nameSpace);
            Log.Debug($" Changing namespace of {symbol.Name}: {symbol.Namespace} -> {newNameSpace}");
            symbol.Namespace = newNameSpace;
        }
    }
}