using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Core.Resource;
using T3.Editor.Compilation;
using T3.Editor.Gui.ChildUi;

// ReSharper disable RedundantNameQualifier

namespace T3.Editor.UiModel;

public partial class UiSymbolData : SymbolData
{
    public UiSymbolData(Assembly operatorAssembly, bool enableLog)
        : base(operatorAssembly)
    {
        Init(enableLog);
    }

    private void Init(bool enableLog)
    {
        foreach (var symbolEntry in SymbolRegistry.Entries)
        {
            var valueInstanceType = symbolEntry.Value.InstanceType;
            if (typeof(IDescriptiveFilename).IsAssignableFrom(valueInstanceType))
            {
                CustomChildUiRegistry.Entries.Add(valueInstanceType, DescriptiveUi.DrawChildUi);
            }
        }

        Load(enableLog);

        Console.WriteLine(@"Updating UI entries...");
        var symbols = SymbolRegistry.Entries;
        foreach (var symbolEntry in symbols)
        {
            UpdateUiEntriesForSymbol(symbolEntry.Value);
        }

        // Create instance of project op, all children are created automatically
        Console.WriteLine(@"Creating home...");
        var homeSymbol = symbols[HomeSymbolId];
        var homeInstanceId = Guid.Parse("12d48d5a-b8f4-4e08-8d79-4438328662f0");
        RootInstance = homeSymbol.CreateInstance(homeInstanceId);
    }

    internal static readonly Guid HomeSymbolId = Guid.Parse("dab61a12-9996-401e-9aa6-328dd6292beb");

    public override void Load(bool enableLog)
    {
        // first load core data
        base.Load(enableLog);

        Console.WriteLine(@"Loading Symbol UIs...");
        var symbolUiFiles = Directory.GetFiles(OperatorTypesFolder, $"*{SymbolUiExtension}", SearchOption.AllDirectories);
        var symbolUiJsons = symbolUiFiles.AsParallel()
                                         .Select(JsonFileResult<SymbolUi>.ReadAndCreate)
                                         .Select(symbolUiJson =>
                                                 {
                                                     var gotSymbolUi = SymbolUiJson.TryReadSymbolUi(symbolUiJson.JToken, symbolUiJson.Guid, out var symbolUi);
                                                     if (!gotSymbolUi)
                                                     {
                                                         Log.Error($"Error reading symbol Ui for {symbolUiJson.Guid} from file \"{symbolUiJson.FilePath}\"");
                                                         return null;
                                                     }

                                                     symbolUiJson.Object = symbolUi;
                                                     return symbolUiJson;
                                                 })
                                         .Where(x => x.ObjectWasSet)
                                         .ToList();

        Console.WriteLine(@"Registering Symbol UIs...");
        foreach (var symbolUiJson in symbolUiJsons)
        {
            var symbolUi = symbolUiJson.Object;
            if (!SymbolUiRegistry.Entries.TryAdd(symbolUi.Symbol.Id, symbolUi))
            {
                Log.Error($"Can't load UI for [{symbolUi.Symbol.Name}] Registry already contains id {symbolUi.Symbol.Id}.");
                continue;
            }

            // if (enableLog)
            //     Log.Debug($"Add UI for {symbolUi.Symbol.Name} {symbolUi.Symbol.Id}");
        }
    }

    public override void SaveAll()
    {
        Log.Debug("Saving...");
        IsSaving = true;

        // Save all t3 and source files
        base.SaveAll();

        // Remove all old ui files before storing to get rid off invalid ones
        // TODO: this also seems dangerous, similar to how the Symbol SaveAll works
        var symbolUiFiles = Directory.GetFiles(OperatorTypesFolder, $"*{SymbolUiExtension}", SearchOption.AllDirectories);
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

        WriteSymbolUis(SymbolUiRegistry.Entries.Values);

        IsSaving = false;
    }

    public static IEnumerable<SymbolUi> GetModifiedSymbolUis()
    {
        return SymbolUiRegistry.Entries.Values.Where(symbolUi => symbolUi.HasBeenModified);
    }

    /// <summary>
    /// Note: This does NOT clean up 
    /// </summary>
    public void SaveModifiedSymbols()
    {
        try
        {
            var modifiedSymbolUis = GetModifiedSymbolUis().ToList();
            Log.Debug($"Saving {modifiedSymbolUis.Count} modified symbols...");

            IsSaving = true;
            ResourceFileWatcher.DisableOperatorFileWatcher(); // Don't update ops if file is written during save

            var modifiedSymbols = modifiedSymbolUis.Select(symbolUi => symbolUi.Symbol).ToList();
            SaveSymbolDefinitionAndSourceFiles(modifiedSymbols);
            WriteSymbolUis(modifiedSymbolUis);
        }
        catch (System.InvalidOperationException e)
        {
            Log.Warning($"Saving failed. Please try to save manually ({e.Message})");
        }

        ResourceFileWatcher.EnableOperatorFileWatcher();
        IsSaving = false;
    }

    private static void WriteSymbolUis(IEnumerable<SymbolUi> symbolUis)
    {
        var resourceManager = ResourceManager.Instance();

        foreach (var symbolUi in symbolUis)
        {
            var symbol = symbolUi.Symbol;
            var filepath = BuildFilepathForSymbol(symbol, SymbolUiExtension);

            using (var sw = new StreamWriter(filepath))
            using (var writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;
                SymbolUiJson.WriteSymbolUi(symbolUi, writer);
            }

            var symbolSourceFilepath = BuildFilepathForSymbol(symbol, SymbolData.SourceExtension);
            var opResource = resourceManager.GetOperatorFileResource(symbolSourceFilepath);
            if (opResource == null)
            {
                // If the source wasn't registered before do this now
                resourceManager.CreateOperatorEntry(symbolSourceFilepath, symbol.Id.ToString(), OperatorUpdating.ResourceUpdateHandler);
            }

            symbolUi.ClearModifiedFlag();
        }
    }

    public bool IsSaving { get; private set; }

    public static void UpdateUiEntriesForSymbol(Symbol symbol)
    {
        if (SymbolUiRegistry.Entries.TryGetValue(symbol.Id, out var symbolUi))
        {
            symbolUi.UpdateConsistencyWithSymbol();
        }
        else
        {
            var newSymbolUi = new SymbolUi(symbol);
            SymbolUiRegistry.Entries.Add(symbol.Id, newSymbolUi);
        }
    }

    public Instance RootInstance { get; private set; }
}