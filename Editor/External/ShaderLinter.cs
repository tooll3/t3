#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using T3.Core.Logging;
using T3.Core.Resource;
using T3.Serialization;

namespace T3.Editor.External;

/// <summary>
/// This is how we make HlslTools work with our shader linking
/// <seealso href="https://github.com/tgjones/HlslTools?tab=readme-ov-file#custom-preprocessor-definitions-and-additional-include-directories"/>
/// <br/>Todo: make generic for supporting other IDEs (jetbrains, vs, vim??)
/// </summary>
internal static class ShaderLinter
{
    private static readonly Dictionary<IResourcePackage, HlslToolsJson> HlslToolsJsons = new();

    private const string FileName = "shadertoolsconfig.json";

    public static void AddPackage(IResourcePackage package, IEnumerable<IResourcePackage>? additionalPackages, bool replaceExisting = false)
    {
        var filePath = Path.Combine(package.ResourcesFolder, FileName);
        var jsonObject = new HlslToolsJson(filePath);
        var resourceFolders = jsonObject.IncludeDirectories;

        resourceFolders.Add(package.ResourcesFolder);

        if (additionalPackages is not null)
        {
            resourceFolders.AddRange(additionalPackages.Select(p => p.ResourcesFolder));
        }

        if (!JsonUtils.TrySaveJson(jsonObject, filePath))
        {
            Log.Error($"{nameof(ShaderLinter)}: failed to save {FileName} to \"{filePath}\"");
            return;
        }

        if (!replaceExisting)
        {
            HlslToolsJsons.Add(package, jsonObject);
        }
        else
        {
            var existing = HlslToolsJsons.SingleOrDefault(x => x.Key.ResourcesFolder == package.ResourcesFolder);
            if (existing.Key != null)
            {
                HlslToolsJsons.Remove(existing.Key);
            }
        }
    }

    public static void DeleteFiles()
    {
        foreach (var json in HlslToolsJsons.Values)
        {
            TryDelete(json.FilePath);
        }
        
        HlslToolsJsons.Clear();
    }

    private static void TryDelete(string filePath)
    {
        try
        {
            File.Delete(filePath);
        }
        catch (Exception e)
        {
            Log.Error($"{nameof(ShaderLinter)}: failed to delete {filePath}: {e.Message}");
        }
    }

    [Serializable]
    private class HlslToolsJson(string filePath)
    {
        [JsonProperty("root")]
        public readonly bool Root = true;
        
        [JsonProperty("hlsl.preprocessorDefinitions")]
        public readonly string[] PreProcessorDefinitions = Array.Empty<string>();

        [JsonProperty("hlsl.additionalIncludeDirectories")]
        public readonly List<string> IncludeDirectories = ["."];

        [JsonProperty("hlsl.virtualDirectoryMappings")]
        public readonly string? VirtualDirectoryMappings;

        [JsonIgnore]
        public string FilePath = filePath;
    }

    public static void RemovePackage(IResourcePackage resourcePackage)
    {
        var json = HlslToolsJsons[resourcePackage];
        var filePath = json.FilePath;
        
        TryDelete(filePath);
        HlslToolsJsons.Remove(resourcePackage);
        
        var resourceFolder = resourcePackage.ResourcesFolder;
        foreach(var dependent in HlslToolsJsons.Values)
        {
            if(dependent.IncludeDirectories.Remove(resourceFolder))
                JsonUtils.TrySaveJson(json, dependent.FilePath);
        }
    }
}