#nullable enable
using System.IO;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Editor.Gui.Windows.Utilities;

namespace T3.Editor.Gui.Graph;

internal static partial class PlayerExporter
{
    private class ExportInfo
    {
        private HashSet<Instance> CollectedInstances { get; } = new();
        public HashSet<Symbol> UniqueSymbols { get; } = new();
        public HashSet<ResourcePath> UniqueResourcePaths { get; } = new();

        public bool TryAddInstance(Instance instance) => CollectedInstances.Add(instance);

        public void TryAddResourcePath(in ResourcePath path) => UniqueResourcePaths.Add(path);

        public bool TryAddSymbol(Symbol symbol) => UniqueSymbols.Add(symbol);

        public void PrintInfo()
        {
            Log.Info($"Collected {CollectedInstances.Count} instances for export in {UniqueSymbols.Count} different symbols:");
            foreach (var resourcePath in UniqueResourcePaths)
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