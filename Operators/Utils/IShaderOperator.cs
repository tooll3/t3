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
        bool updated;

        var shouldUpdate = !SourceIsSourceCode || Source.DirtyFlag.IsDirty || EntryPoint.DirtyFlag.IsDirty || DebugName.DirtyFlag.IsDirty;

        if (!shouldUpdate)
        {
            message = string.Empty;
            return false;
        }

        var source = Source.GetValue(context);
        var entryPoint = EntryPoint.GetValue(context);
        var debugName = DebugName.GetValue(context);

        if (!TryGetDebugName(out message))
        {
            Log.Error($"Failed to update shader \"{debugName}\":\n{message}");
            return false;
        }

        Log.Debug($"Attempting to update shader \"{debugName}\" ({GetType().Name}) with entry point \"{entryPoint}\".");
        if (SourceIsSourceCode)
        {
            var needsNewResource = ShaderResource == null;
            updated = needsNewResource
                          ? TryCreateResource(source, entryPoint, debugName, this, out message)
                          : ShaderResource.TryUpdateFromSource(source, entryPoint, out message);
        }
        else
        {
            var needsNewResource = ShaderResource == null || cachedSource != source;
            updated = needsNewResource
                          ? TryCreateResource(source, entryPoint, debugName, this, out message)
                          : ShaderResource.UpdateFromFile(source, entryPoint, out message);
        }

        if (updated && ShaderResource != null)
        {
            ShaderResource.UpdateDebugName(debugName);
            Shader.Value = ShaderResource.Shader;
        }
        else
        {
            Log.Error($"Failed to update shader \"{debugName}\":\n{message}");
        }

        cachedSource = source;
        return updated;

        bool TryGetDebugName(out string dbgMessage)
        {
            dbgMessage = string.Empty;

            if (SourceIsSourceCode && string.IsNullOrWhiteSpace(debugName))
                debugName = $"{GetType().Name} - {Source.Id}";

            if (!SourceIsSourceCode && string.IsNullOrEmpty(debugName) && !string.IsNullOrEmpty(source))
            {
                try
                {
                    debugName = Path.GetFileNameWithoutExtension(source) + " - " + entryPoint;
                }
                catch (Exception e)
                {
                    dbgMessage = $"Invalid source path for shader: {source}:\n" + e.Message;
                    return false;
                }
            }

            return true;
        }

        static bool TryCreateResource(string source, string entryPoint, string debugName, IShaderOperator<T> shaderOperator, out string errorMessage)
        {
            bool updated;
            var resourceManager = ResourceManager.Instance();
            ShaderResource<T> shaderResource;

            if (shaderOperator.SourceIsSourceCode)
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
                                                                  fileChangedAction: () => shaderOperator.Source.DirtyFlag.Invalidate(),
                                                                  errorMessage: out errorMessage);
            }

            shaderOperator.ShaderResource = shaderResource;
            return updated;
        }
    }
}