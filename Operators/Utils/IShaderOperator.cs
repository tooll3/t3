using System;
using System.IO;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace Operators.Utils;

public interface IShaderOperator<T> where T : class, IDisposable
{
    public Slot<T> Shader { get; }
    public InputSlot<string> Source { get; }
    public InputSlot<string> EntryPoint { get; }
    public InputSlot<string> DebugName { get; }
    bool SourceIsSourceCode { get; }
    protected internal ShaderResource<T> ShaderResource { get; set; }

    internal bool TryUpdateShader(EvaluationContext context, ref string cachedSource, out string message)
    {
        // cache interface values to avoid additional virtual method calls
        bool isSourceCode = SourceIsSourceCode;
        var sourceSlot = Source;
        var entryPointSlot = EntryPoint;
        var debugNameSlot = DebugName;
        
        var shouldUpdate = !isSourceCode || sourceSlot.DirtyFlag.IsDirty || entryPointSlot.DirtyFlag.IsDirty || debugNameSlot.DirtyFlag.IsDirty;

        if (!shouldUpdate)
        {
            message = string.Empty;
            return false;
        }

        var source = sourceSlot.GetValue(context);
        var entryPoint = entryPointSlot.GetValue(context);
        var debugName = debugNameSlot.GetValue(context);
        
        var type = GetType();

        if (!TryGetDebugName(out message, ref debugName))
        {
            Log.Error($"Failed to update shader \"{debugName}\":\n{message}");
            return false;
        }

        //Log.Debug($"Attempting to update shader \"{debugName}\" ({GetType().Name}) with entry point \"{entryPoint}\".");
        
        // Cache ShaderResource to avoid additional virtual method calls
        var shaderResource = ShaderResource;
        var needsNewResource = shaderResource == null;

        if (!isSourceCode)
            needsNewResource = needsNewResource || cachedSource != source;

        bool updated;

        if (needsNewResource)
        {
            updated = TryCreateResource(source, entryPoint, debugName, isSourceCode, Shader, out message, out shaderResource);
            if(updated)
                ShaderResource = shaderResource;
        }
        else
        {
            updated = TryUpdateShaderResource(source, entryPoint, isSourceCode, shaderResource, out message);
        }

        if (updated && shaderResource != null)
        {
            shaderResource.UpdateDebugName(debugName);
            Shader.Value = shaderResource.Shader;
            Shader.DirtyFlag.Invalidate();
        }
        else
        {
            Log.Error($"Failed to update shader \"{debugName}\":\n{message}");
        }

        cachedSource = source;
        return updated;

        bool TryGetDebugName(out string dbgMessage, ref string dbgName)
        {
            dbgMessage = string.Empty;

            if (!string.IsNullOrWhiteSpace(dbgName))
                return true;

            if (isSourceCode)
            {
                dbgName = $"{type.Name}({entryPoint}) - {sourceSlot.Id}";
                return true;
            }

            if (string.IsNullOrWhiteSpace(source))
            {
                dbgMessage = "Source path is empty.";
                return false;
            }

            try
            {
                dbgName = Path.GetFileNameWithoutExtension(source) + " - " + entryPoint;
                return true;
            }
            catch (Exception e)
            {
                dbgMessage = $"Invalid source path for shader: {source}:\n" + e.Message;
                return false;
            }
        }
        
        static bool TryUpdateShaderResource(string source, string entryPoint, bool srcIsSourceCode, ShaderResource<T> resource, out string errorMessage)
        {
            var success = srcIsSourceCode 
                              ? resource.TryUpdateFromSource(source, entryPoint, out errorMessage) 
                              : resource.TryUpdateFromFile(source, entryPoint, out errorMessage);

            return success;
        }

        static bool TryCreateResource(string source, string entryPoint, string debugName, bool isSourceCode, Slot<T> shaderSlot, out string errorMessage, out ShaderResource<T> shaderResource)
        {
            bool updated;
            var resourceManager = ResourceManager.Instance();

            if (isSourceCode)
            {
                updated = resourceManager.TryCreateShaderResourceFromSource(out shaderResource,
                                                                            shaderSource: source,
                                                                            entryPoint: entryPoint,
                                                                            name: debugName,
                                                                            errorMessage: out errorMessage);
            }
            else
            {
                updated = resourceManager.TryCreateShaderResource(out shaderResource,
                                                                  fileName: source,
                                                                  entryPoint: entryPoint,
                                                                  name: debugName,
                                                                  fileChangedAction: () =>
                                                                                     {
                                                                                         //sourceSlot.DirtyFlag.Invalidate();
                                                                                         shaderSlot.DirtyFlag.Invalidate();
                                                                                         //Log.Debug($"Invalidated {sourceSlot}   isDirty: {sourceSlot.DirtyFlag.IsDirty}", sourceSlot.Parent);
                                                                                     },
                                                                  errorMessage: out errorMessage);
            }

            return updated;
        }
    }
}