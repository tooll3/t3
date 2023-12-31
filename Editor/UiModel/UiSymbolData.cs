using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
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
    public UiSymbolData(AssemblyInformation assembly, bool enableLog)
        : base(assembly)
    {
        Init(enableLog);

        var gotProject = TryFindMatchingCSProj(assembly, out var csprojFile);
        if (!gotProject)
            throw new ArgumentException("Could not find matching csproj file", nameof(assembly));

        Folder = Path.GetDirectoryName(csprojFile!.FullName);
        ResourceFileWatcher.AddCodeWatcher(Folder);
        
        SymbolDataByAssemblyLocationEditable.Add(assembly.Path, this);
    }

    private void Init(bool enableLog)
    {
        Load(enableLog);

        Console.WriteLine($@"Updating UI entries for {AssemblyInformation.Name}...");

        Console.WriteLine(@"Registering Symbol UIs...");

        foreach (var symbolUi in SymbolUis)
        {
            var symbol = symbolUi.Symbol;

            RegisterCustomChildUi(symbol);

            if (!SymbolUiRegistry.Entries.TryAdd(symbolUi.Symbol.Id, symbolUi))
            {
                Log.Error($"Can't load UI for [{symbolUi.Symbol.Name}] Registry already contains id {symbolUi.Symbol.Id}.");
            }
            else
            {
                symbolUi.UpdateConsistencyWithSymbol();
                if (enableLog)
                    Log.Debug($"Add UI for {symbolUi.Symbol.Name} {symbolUi.Symbol.Id}");
            }
        }

        var symbolUisById = SymbolUis.ToDictionary(x => x.Symbol.Id, x => x);
        bool containsHome = symbolUisById.ContainsKey(HomeSymbolId);
        // Create instance of project op, all children are created automatically
        if (containsHome)
        {
            if (RootInstance != null)
            {
                throw new Exception("RootInstance already exists");
            }

            Console.WriteLine(@"Creating home...");
            var homeSymbol = symbolUisById[HomeSymbolId].Symbol;
            var homeInstanceId = Guid.Parse("12d48d5a-b8f4-4e08-8d79-4438328662f0");
            RootInstance = homeSymbol.CreateInstance(homeInstanceId);
        }
    }

    public override void Load(bool enableLog)
    {
        // first load core data
        base.Load(enableLog);

        Console.WriteLine($"Loading Symbol UIs from \"{Folder}\"");
        var symbolUiFiles = Directory.GetFiles(Folder, $"*{SymbolUiExtension}", SearchOption.AllDirectories);
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
    }

    public override void SaveAll()
    {
        Log.Debug("Saving...");
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
    public void SaveModifiedSymbols()
    {
        MarkAsSaving();
        try
        {
            var modifiedSymbolUis = _symbolUis.Where(symbolUi => symbolUi.HasBeenModified).ToList();
            Log.Debug($"Saving {modifiedSymbolUis.Count} modified symbols...");

            ResourceFileWatcher.DisableOperatorFileWatcher(Folder); // Don't update ops if file is written during save

            var modifiedSymbols = modifiedSymbolUis.Select(symbolUi => symbolUi.Symbol).ToList();
            SaveSymbolDefinitionAndSourceFiles(modifiedSymbols);
            WriteSymbolUis(modifiedSymbolUis);
        }
        catch (System.InvalidOperationException e)
        {
            Log.Warning($"Saving failed. Please try to save manually ({e.Message})");
        }

        ResourceFileWatcher.EnableOperatorFileWatcher(Folder);
        UnmarkAsSaving();
    }

    private void WriteSymbolUis(IEnumerable<SymbolUi> symbolUis)
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

            var symbolUiResource = resourceManager.GetOperatorFileResource(filepath);
            if (symbolUiResource == null)
            {
                // If the source wasn't registered before do this now
                resourceManager.CreateOperatorEntry(filepath, symbol.Id.ToString(), AssemblyInformation, OperatorUpdating.ResourceUpdateHandler);
            }

            var symbolSourceFilepath = BuildFilepathForSymbol(symbol, SymbolData.SourceExtension);
            var opResource = resourceManager.GetOperatorFileResource(symbolSourceFilepath);
            if (opResource == null)
            {
                // If the source wasn't registered before do this now
                resourceManager.CreateOperatorEntry(symbolSourceFilepath, symbol.Id.ToString(), AssemblyInformation, OperatorUpdating.ResourceUpdateHandler);
            }

            symbolUi.ClearModifiedFlag();
        }
    }

    public void UpdateUiEntriesForSymbol(Symbol symbol)
    {
        if (SymbolUiRegistry.Entries.TryGetValue(symbol.Id, out var symbolUi))
        {
            symbolUi.UpdateConsistencyWithSymbol();
        }
        else
        {
            symbolUi = new SymbolUi(symbol);
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

    private static void RegisterCustomChildUi(Symbol symbol)
    {
        var valueInstanceType = symbol.InstanceType;
        if (typeof(IDescriptiveFilename).IsAssignableFrom(valueInstanceType))
        {
            CustomChildUiRegistry.Entries.TryAdd(valueInstanceType, DescriptiveUi.DrawChildUi);
        }
    }

    internal static readonly Guid HomeSymbolId = Guid.Parse("dab61a12-9996-401e-9aa6-328dd6292beb");

    public static Instance RootInstance { get; private set; }
    public IReadOnlyList<SymbolUi> SymbolUis => _symbolUis;
    private List<SymbolUi> _symbolUis = new();
    
    public static IReadOnlyDictionary<string, UiSymbolData> SymbolDataByAssemblyLocation => SymbolDataByAssemblyLocationEditable;
    private static readonly Dictionary<string, UiSymbolData> SymbolDataByAssemblyLocationEditable = new();

}