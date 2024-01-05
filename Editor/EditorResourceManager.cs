using System.Collections.Generic;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Editor.Compilation;

namespace T3.Editor;

internal class EditorResourceManager : ResourceManager
{
    public EditorResourceManager()
    {
    }

    public uint TrackOperatorFile(string trackedFilePath, Symbol symbol, CsProjectFile parentProject, OperatorResource.UpdateDelegate updateHandler)
    {
        // todo: code below is redundant with all file resources -> refactor
        if (ResourceFileWatcher.HooksForResourceFilepaths.TryGetValue(trackedFilePath, out var fileResource))
        {
            foreach (var id in fileResource.ResourceIds)
            {
                if (_resourcesById.ContainsKey(id))
                    return id;
            }
        }

        var newResourceId = GetNextResourceId();
        var resourceEntry = new OperatorResource(newResourceId, symbol.Id, symbol.InstanceType, parentProject, updateHandler);
        _resourcesById.Add(newResourceId, resourceEntry);

        if (fileResource == null)
        {
            fileResource = new ResourceFileHook(trackedFilePath, new[] { newResourceId });
            var added = ResourceFileWatcher.HooksForResourceFilepaths.TryAdd(trackedFilePath, fileResource);

            if (!added)
                Log.Error($"Can't add resource file hook to '{trackedFilePath}': file already exists");
        }
        else
        {
            // File resource already exists, so just add the id of the new type resource
            fileResource.ResourceIds.Add(newResourceId);
        }

        return newResourceId;
    }
    
    private readonly Dictionary<uint, OperatorResource> _resourcesById = new();
    
    public static EditorResourceManager Instance => (EditorResourceManager)_instance;
}