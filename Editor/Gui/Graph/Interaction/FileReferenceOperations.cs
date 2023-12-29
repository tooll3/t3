using System.Collections.Generic;
using System.IO;
using System.Linq;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Editor.Gui.InputUi.SimpleInputUis;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Interaction
{
    public class FileReferenceOperations
    {
        public static void FixOperatorFilepathsCommand_Executed()
        {
            AssetFiles.Clear();
            ScanAssetDirectory(ResourceManager.ResourcesFolder);
            foreach (var ( key, value) in AssetFiles)
            {
                Log.Debug($"found {key} in {value}");
            }

                
            foreach (var symbol in SymbolRegistry.Entries.Values)
            {
                var symbolUpdated = FindMissingPathsInSymbol(symbol);
                if (symbolUpdated)
                {
                    
                }
            }
        }

        
        private static void ScanAssetDirectory(string path)
        {
            string[] files = new string[0];
            try
            {
                files = Directory.GetFiles(path);
            }
            catch (DirectoryNotFoundException)
            {
                Log.Warning(string.Format($"unable to access directory '{path}'. \nThis could be due to a broken file-link."));
                return;
            }

            foreach (var filepath in files)
            {
                //var basename = filename.Split('\\').Last();
                var basename = Path.GetFileName(filepath);
                if (AssetFiles.ContainsKey(basename))
                {
                    Log.Info($"Ignoring reappearing instance of file: {path}/{basename}");
                }
                else
                {
                    AssetFiles.Add(basename,  Path.Combine(path, basename));
                }
            }

            foreach (var dir in Directory.GetDirectories(path))
            {
                var dirname = dir.Split('\\').Last();
                //var dirname = Path.f;
                ScanAssetDirectory(path + "\\" + dirname);
            }
        }
        
        private static bool FindMissingPathsInSymbol(Symbol symbol)
        {
            var symbolUpdated = false;
            foreach (var symbolChild in symbol.Children)
            {
                foreach (var input in symbolChild.Symbol.InputDefinitions)
                {
                    var symbolChildUi = SymbolUiRegistry.Entries[symbolChild.Symbol.Id];    
                    var inputUi = symbolChildUi.InputUis[input.Id];
                    if (inputUi is not StringInputUi stringInputUi)
                        continue;

                    if (stringInputUi.Usage != StringInputUi.UsageType.FilePath)
                        continue;

                    if (!symbolChild.Inputs.ContainsKey(input.Id))
                        continue;
                    
                    var inputValue = symbolChild.Inputs[input.Id].IsDefault
                                         ? symbolChild.Inputs[input.Id].DefaultValue
                                         : symbolChild.Inputs[input.Id].Value;

                    var stringInputValue = inputValue as InputValue<string>;
                    if (stringInputValue == null)
                        continue;

                    var path = stringInputValue.Value;

                    if (string.IsNullOrEmpty(path))
                        continue;
                    
                    if (File.Exists(path))
                        continue;
                    
                    Log.Warning($"Missing File: {path} in  {symbol.Name}/{symbolChild.ReadableName}.{input.Name}", symbolChild.Id);
                    var basename = Path.GetFileName(path);
                        
                    if (!AssetFiles.ContainsKey(basename))
                        continue;
                        
                    Log.Info($"  -> fixed with: {AssetFiles[basename]}");
                    stringInputValue.Value =AssetFiles[basename];
                    symbol.InvalidateInputInAllChildInstances(input.Id, symbolChild.Id);
                    symbolUpdated = true;
                }
            }

            return symbolUpdated;
        }
        
        private static readonly Dictionary<string, string> AssetFiles = new();
    }
}