#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;

// ReSharper disable ConvertToLocalFunction

namespace T3.Core.Resource;

/// <summary>
/// Creates or loads shaders as "resources" and handles their filehooks, compilation, etc
/// Could do with some simplification - perhaps their arguments should be condensed into a struct?
/// </summary>
public static partial class ResourceManager
{
    internal static bool TryCompileShaderFromSource<TShader>([NotNullWhen(true)] ref TShader? resource, string shaderSource, Instance instance, out string reason,
                                                           string name = "", string entryPoint = "main")
        where TShader : AbstractShader
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"{typeof(TShader).Name}_{instance.SymbolChildId}";
        }

        var compilationArgs = new ShaderCompiler.ShaderCompilationArgs(
                                                                       SourceCode: shaderSource, 
                                                                       EntryPoint: entryPoint, 
                                                                       Owner: instance,
                                                                       Name: name, 
                                                                       OldBytecode: resource?.CompiledBytecode);
        var compiled = ShaderCompiler.TryCompileShaderFromSource(compilationArgs, true, true, out resource, out reason);

        if (!compiled)
        {
            Log.Error($"Failed to compile shader '{name}'");
        }

        return compiled;
    }

    public static Resource<TShader> CreateShaderResource<TShader>(string relativePath, Instance? instance, Func<string> getEntryPoint, Action<TShader?>? onShaderCompiled = null)
        where TShader : AbstractShader
    {
        ArgumentNullException.ThrowIfNull(getEntryPoint, nameof(getEntryPoint));
        TryGenerate<TShader> func = (FileResource fileResource, TShader? currentValue, out TShader? newShader, out string? reason) =>
                                    {
                                        var success = ShaderCompiler.TryGetShaderFromFile(fileResource, ref currentValue, instance, out reason, getEntryPoint());
                                        newShader = currentValue;
                                        onShaderCompiled?.Invoke(currentValue);
                                        return success;
                                    };

        return new Resource<TShader>(relativePath, instance, func);
    }
    
    public static Resource<TShader> CreateShaderResource<TShader>(InputSlot<string> sourceSlot, Func<string> getEntryPoint)
        where TShader : AbstractShader
    {
        ArgumentNullException.ThrowIfNull(getEntryPoint, nameof(getEntryPoint));
        var instance = sourceSlot.Parent;
        TryGenerate<TShader> func = (FileResource fileResource, TShader? currentValue, out TShader? newShader, out string? reason) =>
                                    {
                                        var success = ShaderCompiler.TryGetShaderFromFile(fileResource, ref currentValue, instance, out reason, getEntryPoint());
                                        newShader = currentValue;
                                        return success;
                                    };

        return new Resource<TShader>(sourceSlot, func);
    }
}