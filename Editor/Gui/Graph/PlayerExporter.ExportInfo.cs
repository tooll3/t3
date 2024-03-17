#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Resource;

namespace T3.Editor.Gui.Graph;

public static partial class PlayerExporter
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

        public bool TryAddSharedResource(string relativePath, IReadOnlyList<IResourceContainer>? otherDirs = null)
        {
            var searchDirs = otherDirs ?? Array.Empty<IResourceContainer>();
            if (!ResourceManager.TryResolvePath(relativePath, searchDirs, out var absolutePath, out _))
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
                 var shaderFolder = Path.GetDirectoryName(absolutePath)!;
                 ShaderCompiler.ShaderResourceContainer shaderResourceContainer = new(shaderFolder);
                 var shaderDirs = searchDirs.Append(shaderResourceContainer).Distinct().ToArray();
                 var shaderText = File.ReadAllText(absolutePath);
                 var includeLines = shaderText.Split('\n').Where(l => l.StartsWith("#include")).ToArray();
                 foreach (var line in includeLines)
                 {
                     // get include path without quotes
                     var split = line.Split('"');
                     if (split.Length < 2)
                         continue;
                     
                     var includePath = line.Split('"')[1];
                     TryAddSharedResource(includePath, shaderDirs);
                 }
            }

            return true;
        }
    }
}