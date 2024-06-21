using System;
using System.Collections.Concurrent;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Core.Operator;

/// <summary>
/// An interface for shader operators (PixelShader, VertexShader, etc.)
/// Shader updating can be complex, so this interface is used to provide the common functionality for all of them
/// </summary>
/// <typeparam name="T">The type of shader (i.e. T3.Core.DataTypes.PixelShader)</typeparam>

public interface IShaderCodeOperator<T> where T : AbstractShader
{
    public InputSlot<string> Code { get; }
    public Slot<T> ShaderSlot { get; }
    public InputSlot<string> EntryPoint { get; }
    public InputSlot<string> DebugName { get; }

    public void SetWarning(string message);

    void Initialize()
    {
        Action<EvaluationContext> invalidateShader = context => ShaderSlot.DirtyFlag.Invalidate();
        Code.UpdateAction += invalidateShader;
        EntryPoint.UpdateAction += invalidateShader;
        ShaderSlot.UpdateAction += UpdateShader;
    }

    private void UpdateShader(EvaluationContext context)
    {
        // cache interface values to avoid additional virtual method calls
        var sourceSlot = Code;
        var entryPointSlot = EntryPoint;
        var debugNameSlot = DebugName;

        var debugName = debugNameSlot.GetValue(context);
        var shaderSlot = ShaderSlot;
        var currentShader = shaderSlot.Value;
        if (currentShader != null)
        {
            currentShader.Name = debugName;
        }

        if (!sourceSlot.DirtyFlag.IsDirty && !entryPointSlot.DirtyFlag.IsDirty)
        {
            return;
        }

        var source = sourceSlot.GetValue(context);
        var entryPoint = entryPointSlot.GetValue(context);
        var instance = sourceSlot.Parent;

        if (string.IsNullOrWhiteSpace(source))
        {
            SetWarning("Shader code is empty");
            return;
        }

        if (string.IsNullOrWhiteSpace(entryPoint))
        {
            SetWarning("Entry point is empty");
            return;
        }

        //Log.Debug($"Attempting to update shader \"{debugName}\" ({GetType().Name}) with entry point \"{entryPoint}\".");
        
        var compiled = ResourceManager.TryCompileShaderFromSource(ref currentShader,
                                                                 shaderSource: source,
                                                                 instance: instance,
                                                                 entryPoint: entryPoint,
                                                                 name: debugName,
                                                                 reason: out var errorMessage);
        shaderSlot.Value = currentShader;
        shaderSlot.DirtyFlag.Clear();

        if (!compiled)
        {
            Log.Error($"Failed to update shader \"{debugName}\" in package \"{instance.Symbol.SymbolPackage.AssemblyInformation.Name}\":\n{errorMessage}");
        }
        else
        {
            currentShader.Name = debugName;
        }
        
        SetWarning(errorMessage);
    }
}

public interface IShaderOperator<T> : IDescriptiveFilename where T : AbstractShader
{
    public InputSlot<string> Path { get; }
    InputSlot<string> IDescriptiveFilename.SourcePathSlot => Path;
    public Slot<T> ShaderSlot { get; }
    public InputSlot<string> EntryPoint { get; }
    public InputSlot<string> DebugName { get; }

    public void SetWarning(string message);
    
    protected string CachedEntryPoint { get; set; }
    void OnShaderUpdate(EvaluationContext context, T? shader);
    
    IResourceConsumer Instance => ShaderSlot.Parent;

    void Initialize()
    {
        var resource = ResourceManager.CreateShaderResource<T>(Path, () => CachedEntryPoint);
        ShaderSlot.UpdateAction = context =>
                                  {
                                      if (!Path.DirtyFlag.IsDirty && EntryPoint.DirtyFlag.IsDirty)
                                      {
                                          // we still need to recompile if the entrypoint changes
                                          resource.MarkFileAsChanged();
                                      }
                                      
                                      CachedEntryPoint = EntryPoint.GetValue(context);
                                      
                                      var shader = resource.GetValue(context);
                                      ShaderSlot.Value = shader;
                                      OnShaderUpdate(context, shader);
                                  };
    }
}