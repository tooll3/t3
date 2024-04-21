using System.Diagnostics.CodeAnalysis;
using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.Shaderc;

namespace ImGuiVulkan;

public unsafe class Compiler
{
    private static readonly IncludeResolveFn IncludeResolveFn = IncludeResolve;
    
    private static IncludeResult* IncludeResolve(void* arg0, byte* arg1, int arg2, byte* arg3, UIntPtr arg4)
    {
        return default;
    }
    
    private static readonly IncludeResultReleaseFn IncludeResultReleaseFn = IncludeResultRelease;
    
    private static void IncludeResultRelease(void* arg0, IncludeResult* arg1)
    {
        
    }
    
    public static bool TryCompileShaderFile(string path, string? entryPoint, [NotNullWhen(true)] out ShaderKind? shaderKind, [NotNullWhen(true)] out byte[]? compiledShader)
    {
        var src = File.ReadAllText(path);
        var fileExtension = Path.GetExtension(path);
        var sourceLanguage = fileExtension switch
                                 {
                                     ".glsl" => SourceLanguage.Glsl,
                                     ".hlsl" => SourceLanguage.Hlsl,
                                     _       => throw new Exception("Unknown shader language used - file extension must be .glsl or .hlsl")
                                 };
        
        var shaderKindExtension = Path.GetExtension(path.AsSpan(0, path.Length - fileExtension.Length));
        shaderKind = shaderKindExtension switch
        {
            ".vert" => ShaderKind.VertexShader,
            ".frag" => ShaderKind.FragmentShader,
            ".comp" => ShaderKind.ComputeShader,
            ".geom"  => ShaderKind.GeometryShader,
            _       => throw new Exception("Unknown shader kind - file extension must be .vert, .frag, or .comp")
        };
        
        var shaderName = Path.GetFileName(path);
        
        return TryCompileShaderSourceCode(src, entryPoint, shaderName, shaderKind.Value, sourceLanguage, out compiledShader);
    }
    
    private static bool TryCompileShaderSourceCode(string src, string? entryPoint, string shaderName, ShaderKind shaderKind,
                                             SourceLanguage sourceLanguage,
                                             [NotNullWhen(true)] out byte[]? compiledShader)
    {
        // hacky way of skipping garbage at the beginning of the file - like BOMs, etc, that the compiler hates
        var srcStartIndex = src.IndexOf("#version", StringComparison.Ordinal);
        if (srcStartIndex == -1)
        {
            Console.WriteLine($"Could not find the start of the source code file. It must define \"#version\".");
            compiledShader = null;
            return false;
        }
        
        if (srcStartIndex != 0)
        {
            Console.Write($"Unexpected garbage at the beginning of file \"{shaderName}\". Skipping garbage...");
            src = src[srcStartIndex..];
        }
        
        const string defaultEntryPoint = "main";
        var entryPointName = entryPoint ?? defaultEntryPoint;
        var entryPointNamePtr = SilkMarshal.StringToPtr(entryPointName, NativeStringEncoding.UTF8);
        var errorTag = SilkMarshal.StringToPtr(shaderName, NativeStringEncoding.UTF8);
        
        var api = Shaderc.GetApi();
        var compiler = api.CompilerInitialize();
        var opts = api.CompileOptionsInitialize();
        
        //api.CompileOptionsSetIncludeCallbacks(opts, IncludeResolveFn, IncludeResultReleaseFn)
        
        api.CompileOptionsSetTargetEnv(opts, TargetEnv.Vulkan, (uint)EnvVersion.Vulkan13);
        
        api.CompileOptionsSetSourceLanguage(opts, sourceLanguage);
        
        var srcPtr = SilkMarshal.StringToPtr(src, NativeStringEncoding.UTF8);
        
        // preprocess shader text - i.e. expand macros and get includes
        var preprocessedResultPtr = api.CompileIntoPreprocessedText(compiler: compiler,
                                                                    source_text: (byte*) srcPtr,
                                                                    source_text_size: (uint)src.Length,
                                                                    shader_kind: shaderKind,
                                                                    input_file_name: (byte*)errorTag,
                                                                    entry_point_name: (byte*)entryPointNamePtr,
                                                                    additional_options: opts);
        
        // check if preprocessing succeeded
        var preProcessStatus = api.ResultGetCompilationStatus(preprocessedResultPtr);
        if (preProcessStatus != CompilationStatus.Success)
        {
            Console.WriteLine($"PreProcess: Compilation of {shaderName} failed: {preProcessStatus}");
            LogErrorsAndRelease(api, preprocessedResultPtr);
            SilkMarshal.FreeString(srcPtr);
            compiledShader = null;
            return false;
        }
        
        // finally compile the shader into spv - aka something we can use
        var preProcessedSrc = api.ResultGetBytes(preprocessedResultPtr);
        var preProcessedLen = api.ResultGetLength(preprocessedResultPtr);
        
        var resultPtr = api.CompileIntoSpv(compiler: compiler,
                                           source_text: preProcessedSrc,
                                           source_text_size: preProcessedLen,
                                           shader_kind: shaderKind,
                                           input_file_name: (byte*)errorTag,
                                           entry_point_name: (byte*)entryPointNamePtr,
                                           additional_options: opts);
        
        var status = api.ResultGetCompilationStatus(resultPtr);
        SilkMarshal.FreeString(srcPtr);
        
        if (status != CompilationStatus.Success)
        {
            var preProcessedSrcStr = SilkMarshal.PtrToString((IntPtr)preProcessedSrc);
            Console.WriteLine($"Preprocessed source:\n--------------------\n\n{preProcessedSrcStr}\n\n--------------------\n");
            api.ResultRelease(preprocessedResultPtr);
            LogErrorsAndRelease(api, resultPtr);
            
            compiledShader = null;
            return false;
        }
        
        api.ResultRelease(preprocessedResultPtr);
        
        // get the compiled shader and copy it into a byte array
        var compilationSpv = api.ResultGetBytes(resultPtr);
        var compiledBytesLen = (ulong)api.ResultGetLength(resultPtr);
        compiledShader = new byte[compiledBytesLen];
        fixed (byte* bytesPtr = compiledShader)
        {
            Buffer.MemoryCopy(compilationSpv, bytesPtr, compiledBytesLen, compiledBytesLen);
        }
        
        Console.WriteLine($"Successfully compiled {shaderName}");
        
        Release(api, resultPtr);
        return true;
        
        
        void LogErrorsAndRelease(Shaderc apiToRelease, CompilationResult* resultToRelease)
        {
            var message = apiToRelease.ResultGetErrorMessage(resultToRelease);
            var messageString = SilkMarshal.PtrToString((nint)message);
            Console.WriteLine(messageString);
            Release(apiToRelease, resultToRelease);
        }
        void Release(Shaderc apiToRelease, CompilationResult* resultToRelease)
        {
            SilkMarshal.Free(entryPointNamePtr);
            SilkMarshal.Free(errorTag);
            apiToRelease.CompilerRelease(compiler);
            apiToRelease.CompileOptionsRelease(opts);
            
            apiToRelease.ResultRelease(resultToRelease);
            
            apiToRelease.Dispose();
        }
    }
}