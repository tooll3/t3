#nullable enable
using System;
using System.Collections.Generic;
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
        public List<ResourcePath> UniqueResourcePaths { get; } = new();

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

        public bool TryAddSharedResource(string relativePath, IEnumerable<string>? otherDirs = null)
        {
            otherDirs ??= Array.Empty<string>();
            if (!ResourceManager.TryResolvePath(relativePath, out var absolutePath, otherDirs))
            {
                Log.Error($"Can't find file: {relativePath}");
                return false;
            }

            TryAddResourcePath(new ResourcePath(relativePath, absolutePath));

            // Copy related font textures
            if (relativePath.EndsWith(".fnt"))
            {
                var relativePathPng = relativePath.Replace(".fnt", ".png");
                var absolutePathPng = absolutePath.Replace(".fnt", ".png");
                TryAddResourcePath(new ResourcePath(relativePathPng, absolutePathPng));
            }

            return true;
        }
    }
}