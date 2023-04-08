using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Stats;

// ReSharper disable RedundantNameQualifier

namespace T3.Core.Resource
{
    
    public partial class Model
    {
        public Assembly OperatorsAssembly { get; }

        public Model(Assembly operatorAssembly)
        {
            OperatorsAssembly = operatorAssembly;
            _updateCounter = new OpUpdateCounter();
        }

        private Dictionary<Guid, JsonFileResult<Symbol>> _symbolJsonTokens = new();

        public virtual void Load(bool enableLog)
        {
            var symbolFiles = Directory.GetFiles(OperatorTypesFolder, $"*{SymbolExtension}", SearchOption.AllDirectories);

            _symbolJsonTokens = symbolFiles.AsParallel()
                                           .Select(JsonFileResult<Symbol>.ReadAndCreate)
                                           .ToDictionary(x => x.Guid, x => x);
            
            _ = _symbolJsonTokens.AsParallel()
                                 .Select(TryReadSymbolFromJsonFileResult)
                                 .ToList(); // Execute and bring back to main thread

            foreach (var idJsonTokenPair in _symbolJsonTokens )
            {
                var jsonResult = idJsonTokenPair.Value;
                if (jsonResult.ObjectWasSet)
                {
                    SymbolRegistry.Entries.TryAdd(jsonResult.Object.Id, jsonResult.Object);
                }
            }

            // Check if there are symbols without a file, if yes add these
            var instanceTypesWithoutFile = (from type in OperatorsAssembly.ExportedTypes
                                 where type.IsSubclassOf(typeof(Instance))
                                 where !type.IsGenericType
                                 select type).ToHashSet();

            foreach (var symbol in SymbolRegistry.Entries.Values)
            {
                instanceTypesWithoutFile.Remove(symbol.InstanceType);
            }

            foreach (var newType in instanceTypesWithoutFile)
            {
                var typeNamespace = newType.Namespace;
                if (string.IsNullOrWhiteSpace(typeNamespace))
                {
                    Log.Error($"Null or empty namespace of type {newType.Name}");
                    continue;
                } 
                
                var @namespace = _innerNamespace.Replace(newType.Namespace ?? string.Empty, "").ToLower();
                var idFromNamespace = _idFromNamespace
                                     .Match(newType.Namespace ?? string.Empty).Value
                                     .Replace('_', '-');

                Debug.Assert(!string.IsNullOrWhiteSpace(idFromNamespace));
                var symbol = new Symbol(newType, Guid.Parse(idFromNamespace))
                                 {
                                     Namespace = @namespace,
                                     Name = newType.Name
                                 };

                var added = SymbolRegistry.Entries.TryAdd(symbol.Id, symbol);
                if (!added)
                {
                    Log.Error($"Ignoring redefinition symbol {symbol.Name}. Please fix multiple definitions in Operators/Types/ folder");
                    continue;
                }
                
                if(enableLog)
                    Log.Debug($"new added symbol: {newType}");
            }

            (bool success, Symbol symbol) TryReadSymbolFromJsonFileResult(KeyValuePair<Guid, JsonFileResult<Symbol>> idJsonResultPair)
            {
                var jsonInfo = idJsonResultPair.Value;
                var success = SymbolJson.TryReadSymbol(this, jsonInfo.Guid, jsonInfo.JToken, out var symbol, allowNonOperatorInstanceType: false);
                if (success)
                {
                    // This jsonInfo.Object can be read/assigned outside of this thread, so we should lock it 
                    lock (jsonInfo)
                    {
                        jsonInfo.Object = symbol;
                    }
                }
                else
                {
                    Log.Warning($"Failed to load symbol {jsonInfo.Guid}");
                }

                return (success, symbol);
            }
        }

        private readonly Regex _innerNamespace = new Regex(@".Id_(\{){0,1}[0-9a-fA-F]{8}_[0-9a-fA-F]{4}_[0-9a-fA-F]{4}_[0-9a-fA-F]{4}_[0-9a-fA-F]{12}(\}){0,1}",
                                                           RegexOptions.IgnoreCase);

        private readonly Regex _idFromNamespace = new Regex(@"(\{){0,1}[0-9a-fA-F]{8}_[0-9a-fA-F]{4}_[0-9a-fA-F]{4}_[0-9a-fA-F]{4}_[0-9a-fA-F]{12}(\}){0,1}",
                                                            RegexOptions.IgnoreCase);

        internal bool TryReadSymbolWithId(Guid symbolId, out Symbol symbol)
        {
            var hasJsonInfo = _symbolJsonTokens.TryGetValue(symbolId, out var jsonInfo); // todo: TryGet
            if (!hasJsonInfo)
            {
                symbol = null;
                return false;
            }

            if (jsonInfo.Object is null)
            {
                var success = SymbolJson.TryReadSymbol(this, jsonInfo.Guid, jsonInfo.JToken, out symbol, false);
                if (!success)
                    return false;
                
                jsonInfo.Object = symbol;

                return true;
            }

            symbol = jsonInfo.Object;
            return true;
        }
        
        public virtual void SaveAll()
        {
            ResourceFileWatcher.DisableOperatorFileWatcher(); // don't update ops if file is written during save
            
            // todo: this sounds like a dangerous step. we should overwrite these files by default and can check which files are not overwritten to delete others?
            RemoveAllSymbolFiles(); 
            SortAllSymbolSourceFiles();
            SaveSymbolDefinitionAndSourceFiles(SymbolRegistry.Entries.Values);

            ResourceFileWatcher.EnableOperatorFileWatcher();
        }

        protected static void SaveSymbolDefinitionAndSourceFiles(IEnumerable<Symbol> valueCollection)
        {
            foreach (var symbol in valueCollection)
            {
                var filepath = BuildFilepathForSymbol(symbol, SymbolExtension);

                using (var sw = new StreamWriter(filepath))
                using (var writer = new JsonTextWriter(sw) { Formatting = Formatting.Indented })
                {
                    SymbolJson.WriteSymbol(symbol, writer);
                }

                if (!string.IsNullOrEmpty(symbol.PendingSource))
                {
                    WriteSymbolSourceToFile(symbol);
                }
            }
        }

        private static void SortAllSymbolSourceFiles()
        {
            // Move existing source files to correct namespace folder
            var sourceFiles = Directory.GetFiles(OperatorTypesFolder, $"*{SourceExtension}", SearchOption.AllDirectories);
            foreach (var sourceFilePath in sourceFiles)
            {
                var classname = Path.GetFileNameWithoutExtension(sourceFilePath);
                var symbol = SymbolRegistry.Entries.Values.SingleOrDefault(s => s.Name == classname);
                if (symbol == null)
                {
                    Log.Warning($"Skipping unregistered source file {sourceFilePath}");
                    continue;
                }

                var targetFilepath = BuildFilepathForSymbol(symbol, SourceExtension);
                if (sourceFilePath == targetFilepath)
                    continue;

                Log.Debug($" Moving {sourceFilePath} -> {targetFilepath} ...");
                try
                {
                    File.Move(sourceFilePath, targetFilepath);
                }
                catch (Exception e)
                {
                    Log.Warning("Failed to write source file '" + sourceFilePath + "': " + e);
                }
            }
        }

        private static void RemoveAllSymbolFiles()
        {
            // Remove all old t3 files before storing to get rid off invalid ones
            var symbolFiles = Directory.GetFiles(OperatorTypesFolder, $"*{SymbolExtension}", SearchOption.AllDirectories);
            foreach (var symbolFilePath in symbolFiles)
            {
                try
                {
                    File.Delete(symbolFilePath);
                }
                catch (Exception e)
                {
                    Log.Warning("Failed to deleted file '" + symbolFilePath + "': " + e);
                }
            }
        }

        // public void SaveModifiedSymbol(Symbol symbol)
        // {
        //     RemoveObsoleteSymbolFiles(symbol);
        //
        //     var symbolJson = new SymbolJson();
        //     
        //     var filepath = BuildFilepathForSymbol(symbol, SymbolExtension);
        //
        //     using (var sw = new StreamWriter(filepath))
        //     using (var writer = new JsonTextWriter(sw))
        //     {
        //         symbolJson.Writer = writer;
        //         symbolJson.Writer.Formatting = Formatting.Indented;
        //         symbolJson.WriteSymbol(symbol);
        //     }
        //
        //     if (!string.IsNullOrEmpty(symbol.PendingSource))
        //     {
        //         WriteSymbolSourceToFile(symbol);
        //     }
        // }

        private static void RemoveDeprecatedSymbolFiles(Symbol symbol)
        {
            if (string.IsNullOrEmpty(symbol.DeprecatedSourcePath))
                return;

            foreach (var fileExtension in OperatorFileExtensions)
            {
                var sourceFilepath = Path.Combine(OperatorTypesFolder, symbol.DeprecatedSourcePath + "_" + symbol.Id + fileExtension);
                try
                {
                    File.Delete(sourceFilepath);
                }
                catch (Exception e)
                {
                    Log.Warning("Failed to deleted file '" + sourceFilepath + "': " + e);
                }
            }

            symbol.DeprecatedSourcePath = String.Empty;
        }


        private static void WriteSymbolSourceToFile(Symbol symbol)
        {
            var sourcePath = BuildFilepathForSymbol(symbol, SourceExtension);
            using (var sw = new StreamWriter(sourcePath))
            {
                sw.Write(symbol.PendingSource);
            }

            // Remove old source file and its entry in project
            if (!string.IsNullOrEmpty(symbol.DeprecatedSourcePath))
            {
                if (symbol.DeprecatedSourcePath == Model.BuildFilepathForSymbol(symbol, SourceExtension))
                {
                    Log.Warning($"Attempted to deprecated valid source file: {symbol.DeprecatedSourcePath}");
                    symbol.DeprecatedSourcePath = string.Empty;
                    return;
                }
                File.Delete(symbol.DeprecatedSourcePath);

                // Adjust path of file resource
                ResourceManager.RenameOperatorResource(symbol.DeprecatedSourcePath, sourcePath);

                symbol.DeprecatedSourcePath = string.Empty;
            }
            symbol.PendingSource = null;
        }

        #region File path handling
        private static string GetSubDirectoryFromNamespace(string symbolNamespace)
        {
            var trimmed = symbolNamespace.Trim().Replace(".", "\\");
            return trimmed;
        }
        
        private static string BuildAndCreateFolderFromNamespace(string symbolNamespace)
        {
            var directory = Path.Combine(OperatorTypesFolder, GetSubDirectoryFromNamespace(symbolNamespace));
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return directory;
        }

        public static string BuildFilepathForSymbol(Symbol symbol, string extension)
        {
            var dir = BuildAndCreateFolderFromNamespace(symbol.Namespace);
            return extension == SourceExtension
                       ? Path.Combine(dir, symbol.Name + extension)
                       : Path.Combine(dir, symbol.Name + "_" + symbol.Id + extension);
        }
        #endregion

        private static OpUpdateCounter _updateCounter;

        public const string SourceExtension = ".cs";
        private const string SymbolExtension = ".t3";
        protected const string SymbolUiExtension = ".t3ui";
        public const string OperatorTypesFolder = @"Operators\Types\";

        private static readonly List<string> OperatorFileExtensions = new()
                                                                           {
                                                                               SymbolExtension,
                                                                               SymbolUiExtension,
                                                                               SourceExtension,
                                                                           };
    }
}