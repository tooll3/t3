#nullable enable
using System.IO;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Editor.Gui.Windows.Utilities;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph;

internal static partial class PlayerExporter
{
    private class ExportInfo
    {
        public IReadOnlyCollection<ResourcePath> ResourcePaths => _resourcePaths;
        public IEnumerable<SymbolPackage> SymbolPackages => _symbolPackages.Keys;

        private readonly HashSet<Symbol> _symbols = new();
        private readonly Dictionary<SymbolPackage, List<Symbol>> _symbolPackages = new();
        private readonly HashSet<ResourcePath> _resourcePaths = new();
        private readonly HashSet<Instance> _collectedInstances = new();

        public bool TryAddInstance(Instance instance) => _collectedInstances.Add(instance);

        public void TryAddResourcePath(in ResourcePath path) => _resourcePaths.Add(path);

        public bool TryAddSymbol(Symbol symbol)
        {
            Console.WriteLine("Including symbol: " + symbol.Name);
            if(!_symbols.Add(symbol))
                return false;
            
            var package = symbol.SymbolPackage;
            if (!_symbolPackages.TryGetValue(package, out var symbols))
            {
                symbols = new List<Symbol>();
                _symbolPackages.Add(package, symbols);
            }
            
            symbols.Add(symbol);

            foreach(var child in symbol.Children.Values)
            {
                TryAddSymbol(child.Symbol);
            }

            return true;
        }

        public void PrintInfo()
        {
            Log.Info($"Collected {_collectedInstances.Count} instances for export in {_symbols.Count} different symbols:");
            foreach (var resourcePath in ResourcePaths)
            {
                Log.Info($"  {resourcePath}");
            }
        }

        public bool TryAddSharedResource(string relativePath, IReadOnlyList<IResourcePackage>? otherDirs = null)
        {
            var searchDirs = otherDirs ?? Array.Empty<IResourcePackage>();
            var tempResourceConsumer = new TempResourceConsumer(searchDirs);
            if (!ResourceManager.TryResolvePath(relativePath, tempResourceConsumer, out var absolutePath, out _))
            {
                Log.Error($"Can't find file: {relativePath}");
                return false;
            }
            
            
            relativePath = relativePath.Replace("\\", "/");
            absolutePath = absolutePath.Replace("\\", "/");

            TryAddResourcePath(new ResourcePath(relativePath, absolutePath));

            // Copy related font textures
            if (relativePath.EndsWith(".fnt", StringComparison.OrdinalIgnoreCase))
            {
                var relativePathPng = relativePath.Replace(".fnt", ".png");
                var absolutePathPng = absolutePath.Replace(".fnt", ".png");
                TryAddResourcePath(new ResourcePath(relativePathPng, absolutePathPng));
            }

            // search for shader includes
            if (absolutePath.EndsWith(".hlsl", StringComparison.OrdinalIgnoreCase))
            {
                var fileInfo = new FileInfo(absolutePath);
                ShaderCompiler.ShaderResourcePackage shaderResourcePackage = new(fileInfo);
                var shaderDirs = searchDirs.Append(shaderResourcePackage).Distinct().ToArray();
                var shaderText = File.ReadAllText(absolutePath);
                foreach (var includePath in ShaderCompiler.GetIncludesFrom(shaderText))
                {
                    TryAddSharedResource(includePath, shaderDirs);
                }
            }

            return true;
        }
    }
}