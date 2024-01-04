using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using T3.Core.Compilation;
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
    public UiSymbolData(AssemblyInformation assembly)
        : base(assembly)
    {
        var gotProject = TryFindMatchingCSProj(assembly, out var csprojFile);
        if (!gotProject)
            throw new ArgumentException($"Could not find matching csproj file for {assembly.Name}", nameof(assembly));

        Folder = Path.GetDirectoryName(csprojFile!.FullName);

        SymbolDataByAssemblyEditable.Add(assembly, this);
    }

    public void RegisterUiSymbols(bool enableLog)
    {
        Log.Debug($@"{AssemblyInformation.Name}: Registering UI entries...");

        var dictionary = SymbolUiRegistry.Entries;

        foreach (var symbolUi in _symbolUis)
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

    public static bool TryCreateHome()
    {
        var gotHome = SymbolOwners.TryGetValue(HomeSymbolId, out var homeSymbolOwner);
        if (!gotHome)
        {
            return false;
        }
        
        var homeUiSymbolData = (UiSymbolData)homeSymbolOwner;
        var symbolUis = homeUiSymbolData._symbolUis;

        var symbolUisById = symbolUis.ToDictionary(x => x.Symbol.Id, x => x);
        bool containsHome = symbolUisById.ContainsKey(HomeSymbolId);
        if (!containsHome)
        {
            Log.Error($"Could not find home symbol for {homeUiSymbolData.AssemblyInformation.Name} - something is wrong.");
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
        return true;
    }

    public void LoadUiFiles()
    {
        Log.Debug($"{AssemblyInformation.Name}: Loading Symbol UIs from \"{Folder}\"");
        var symbolUiFiles = Directory.EnumerateFiles(Folder, $"*{SymbolUiExtension}", SearchOption.AllDirectories);
        _symbolUis = symbolUiFiles.AsParallel()
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
                                  .Where(result => result?.Object != null)
                                  .Select(result => result.Object)
                                  .ToList();

        Log.Debug($"{AssemblyInformation.Name}: Loaded {_symbolUis.Count} symbol UIs");
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

        WriteSymbolUis(_symbolUis);

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
            var modifiedSymbolUis = _symbolUis.Where(symbolUi => symbolUi.HasBeenModified).ToList();
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
        var resourceManager = ResourceManager.Instance();

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

            var symbolUiResource = resourceManager.GetOperatorFileResource(symbolFilePath);
            if (symbolUiResource == null)
            {
                // If the source wasn't registered before do this now
                resourceManager.CreateOperatorEntry(symbolFilePath, symbol.Id.ToString(), AssemblyInformation, OperatorUpdating.ResourceUpdateHandler);
            }

            var symbolSourceFilepath = BuildFilepathForSymbol(symbol, SymbolData.SourceCodeExtension);
            var opResource = resourceManager.GetOperatorFileResource(symbolSourceFilepath);
            if (opResource == null)
            {
                // If the source wasn't registered before do this now
                resourceManager.CreateOperatorEntry(symbolSourceFilepath, symbol.Id.ToString(), AssemblyInformation, OperatorUpdating.ResourceUpdateHandler);
            }

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
            _symbolUis.Add(symbolUi);
        }
    }

    private static bool TryFindMatchingCSProj(AssemblyInformation assembly, out FileInfo csprojFile)
    {
        var assemblyNameString = assembly.Name;
        if (assemblyNameString == null)
            throw new ArgumentException("Assembly name is null", nameof(assembly));

        csprojFile = Directory.GetFiles(OperatorDirectoryName, "*.csproj", SearchOption.AllDirectories)
                              .Select(path => new FileInfo(path))
                              .FirstOrDefault(file =>
                                              {
                                                  var name = Path.GetFileNameWithoutExtension(file.Name);
                                                  return name == assemblyNameString;
                                              });

        return csprojFile != null;
    }

    public override void AddSymbol(Symbol newSymbol)
    {
        base.AddSymbol(newSymbol);
        UpdateUiEntriesForSymbol(newSymbol);
        RegisterCustomChildUi(newSymbol);
    }

    public void AddSymbol(Symbol newSymbol, SymbolUi symbolUi)
    {
        base.AddSymbol(newSymbol);
        UpdateUiEntriesForSymbol(newSymbol, symbolUi);
        RegisterCustomChildUi(newSymbol);
    }

    private static void RegisterCustomChildUi(Symbol symbol)
    {
        var valueInstanceType = symbol.InstanceType;
        if (typeof(IDescriptiveFilename).IsAssignableFrom(valueInstanceType))
        {
            CustomChildUiRegistry.Entries.TryAdd(valueInstanceType, DescriptiveUi.DrawChildUi);
        }
    }

    internal static readonly Guid HomeSymbolId = Guid.Parse("dab61a12-9996-401e-9aa6-328dd6292beb");
    private static readonly Guid HomeInstanceId = Guid.Parse("12d48d5a-b8f4-4e08-8d79-4438328662f0");

    public static Instance RootInstance { get; private set; }
    private List<SymbolUi> _symbolUis = new();

    public static IReadOnlyDictionary<AssemblyInformation, UiSymbolData> SymbolDataByAssembly => SymbolDataByAssemblyEditable;
    private static readonly Dictionary<AssemblyInformation, UiSymbolData> SymbolDataByAssemblyEditable = new();
}