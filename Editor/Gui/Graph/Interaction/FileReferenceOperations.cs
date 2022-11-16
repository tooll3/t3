using System.Collections.Generic;
using System.IO;
using System.Linq;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using Editor.Gui.InputUi;
using Editor.Gui.InputUi.SimpleInputUis;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace Editor.Gui.Graph.Interaction
{
    public class FileReferenceOperations
    {
        private static void FindMissingPathsInSymbol(Symbol symbol)
        {
            foreach (var symbolChild in symbol.Children)
            {
                //var symbolUi = SymbolUiRegistry.Entries[symbol.Id];

                foreach (var input in symbolChild.Symbol.InputDefinitions)
                {
                    var symbolChildUi = SymbolUiRegistry.Entries[symbolChild.Symbol.Id];    
                    var inputUi = symbolChildUi.InputUis[input.Id];
                    if (!(inputUi is StringInputUi stringInputUi))
                        continue;

                    if (stringInputUi.Usage != StringInputUi.UsageType.FilePath)
                        continue;

                    if (!symbolChild.InputValues.ContainsKey(input.Id))
                        continue;
                    
                    var inputValue = symbolChild.InputValues[input.Id].IsDefault
                                         ? symbolChild.InputValues[input.Id].DefaultValue
                                         : symbolChild.InputValues[input.Id].Value;

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
                FindMissingPathsInSymbol(symbol);
            }
        }

        private static readonly Dictionary<string, string> AssetFiles = new Dictionary<string, string>();
    }
}