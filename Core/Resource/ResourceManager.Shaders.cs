#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using T3.Core.Logging;
using T3.Core.Operator;

namespace T3.Core.Resource;

/// <summary>
/// Creates or loads shaders as "resources" and handles their filehooks, compilation, etc
/// Could do with some simplification - perhaps their arguments should be condensed into a struct?
/// </summary>
public sealed partial class ResourceManager
{
    public bool TryCreateShaderResourceFromSource<TShader>(out ShaderResource<TShader> resource, string shaderSource, Instance instance,
                                                           out string errorMessage,
                                                           string name = "", string entryPoint = "main")
        where TShader : class, IDisposable
    {
        var resourceId = GetNextResourceId();
        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"{typeof(TShader).Name}_{resourceId}";
        }

        var compiled = ShaderCompiler.Instance.TryCreateShaderResourceFromSource<TShader>(shaderSource: shaderSource,
                                                                                          name: name,
                                                                                          directories: instance.AvailableResourcePackages,
                                                                                          entryPoint: entryPoint,
                                                                                          resourceId: resourceId,
                                                                                          resource: out var newResource,
                                                                                          errorMessage: out errorMessage);

        if (compiled)
        {
            ResourcesById.TryAdd(newResource.Id, newResource);
        }
        else
        {
            Log.Error($"Failed to compile shader '{name}'");
        }

        resource = newResource;
        return compiled;
    }

    public bool TryCreateShaderResource<TShader>(out ShaderResource<TShader>? resource, Instance? instance, string relativePath,
                                                 out string errorMessage,
                                                 string name = "", string entryPoint = "main", Action? fileChangedAction = null)
        where TShader : class, IDisposable
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            resource = null;
            errorMessage = "Empty file name";
            return false;
        }

        if (!TryResolvePath(relativePath, instance?.AvailableResourcePackages, out var path, out var resourceContainer))
        {
            resource = null;
            errorMessage = $"Path not found: '{relativePath}' (Resolved to '{path}').";
            return false;
        }

        var fileInfo = new FileInfo(path);
        if (string.IsNullOrWhiteSpace(name))
            name = fileInfo.Name;
        
        if(string.IsNullOrWhiteSpace(entryPoint))
            entryPoint = "main";
        
        List<IResourcePackage> compilationReferences = new();
        ResourceFileWatcher? fileWatcher = null;
        ResourceFileHook? fileHook = null;

        if (instance != null)
            compilationReferences.AddRange(instance.AvailableResourcePackages);

        if (resourceContainer != null)
        {
            compilationReferences.Add(resourceContainer);
            fileWatcher = resourceContainer.FileWatcher;
            if (TryFindExistingResource(fileWatcher, relativePath, fileChangedAction, entryPoint, out fileHook, out var potentialResource))
            {
                resource = potentialResource;
                errorMessage = string.Empty;
                return true;
            }
        }


        // need to create
        var resourceId = GetNextResourceId();
        var compiled = ShaderCompiler.Instance.TryCreateShaderResourceFromFile(srcFile: path,
                                                                               entryPoint: entryPoint,
                                                                               name: name,
                                                                               resourceId: resourceId,
                                                                               resource: out resource,
                                                                               errorMessage: out errorMessage,
                                                                               resourceDirs: compilationReferences);

        if (!compiled)
        {
            Log.Error($"Failed to compile shader '{path}'");
            return false;
        }

        ResourcesById.TryAdd(resource!.Id, resource);
        if (resourceContainer == null || fileWatcher == null)
            return true;

        if (fileHook == null)
        {
            fileHook = new ResourceFileHook(path, new[] { resourceId });
            fileWatcher.HooksForResourceFilePaths.TryAdd(relativePath, fileHook);
        }

        if (fileChangedAction != null)
        {
            fileHook.FileChangeAction -= fileChangedAction;
            fileHook.FileChangeAction += fileChangedAction;
        }

        return true;

        static bool TryFindExistingResource(ResourceFileWatcher? fileWatcher, string relativePath, Action? fileChangedAction, string entryPoint, out ResourceFileHook? fileHook, out ShaderResource<TShader>? resource)
        {
            if (fileWatcher == null)
            {
                fileHook = null;
                resource = null;
                return false;
            }
            
            if(!fileWatcher.HooksForResourceFilePaths.TryGetValue(relativePath, out fileHook))
            {
                resource = null;
                return false;
            }

            var resourceIds = fileHook.ResourceIds;
            var count = resourceIds.Count;
            for (var index = 0; index < count; index++)
            {
                var id = resourceIds[index];
                var resourceById = ResourcesById[id];
                if (resourceById is not ShaderResource<TShader> shaderResource || shaderResource.EntryPoint != entryPoint)
                    continue;

                if (fileChangedAction != null)
                {
                    fileHook.FileChangeAction -= fileChangedAction;
                    fileHook.FileChangeAction += fileChangedAction;
                }

                resource = shaderResource;
                return true;
            }

            resource = null;
            return false;
        }
    }
}