#nullable enable
using System.Collections.Frozen;
using T3.Core.Compilation;
using T3.Core.Model;
using T3.Core.Resource;
using T3.Editor.UiModel;

namespace T3.Editor.Compilation;

internal sealed partial class CsProjectFile
{
    private static readonly FrozenDictionary<PropertyType, string> DefaultProperties =
        new[]
                {
                    (Type: PropertyType.TargetFramework, Value: "net8.0-windows"),
                    (Type: PropertyType.DisableTransitiveProjectReferences, Value: "true"),
                    (Type: PropertyType.VersionPrefix, Value: "1.0.0"),
                    (Type: PropertyType.Nullable, Value: "enable"),
                    (Type: PropertyType.EditorVersion, Value: Program.Version.ToBasicVersionString()),
                    (Type: PropertyType.IsEditorOnly, Value: "false"),
                    (Type: PropertyType.ImplicitUsings, Value: "disabled")
                }
        .ToFrozenDictionary(keySelector: x => x.Type, elementSelector: x => x.Value);
    
    private static readonly TagValue[] DefaultReferenceTags = [new TagValue(Tag: MetadataTagType.Private, Value: "true", AddAsAttribute: true)];
    private static readonly Reference[] DefaultReferences =
        [
            new Reference(type: ItemType.EditorReference, include: "Core.dll", tags: DefaultReferenceTags),
            new Reference(type: ItemType.EditorReference, include: "Logging.dll", tags: DefaultReferenceTags),
            new Reference(type: ItemType.EditorReference, include: "SharpDX.dll", tags: DefaultReferenceTags),
            new Reference(type: ItemType.EditorReference, include: "SharpDX.Direct3D11.dll", tags: DefaultReferenceTags),
            new Reference(type: ItemType.EditorReference, include: "SharpDX.DXGI.dll", tags: DefaultReferenceTags),
            new Reference(type: ItemType.EditorReference, include: "SharpDX.Direct2D1.dll", tags: DefaultReferenceTags),
        ];

    // Note : we are trying to stay platform-agnostic with directories, and so we use unix path separators
    private static readonly Condition ReleaseConfigCondition = new(ConditionVarName: "Configuration", RequiredValue: "Release", IfEqual: true);
    private const string IncludeAllStr = "**";
    private static readonly string[] ExcludeFoldersFromOutput = [CreateIncludePath(args: ["bin", IncludeAllStr]), CreateIncludePath(args: ["obj", IncludeAllStr])];
    private const string FileIncludeFmt = IncludeAllStr + @"{0}";
    private const string DependenciesFolder = "dependencies";
    private static readonly ContentInclude.Group[] DefaultContent =
        [
            new ContentInclude.Group(Condition: null, Content: new ContentInclude(include: CreateIncludePath(args: [".", DependenciesFolder, IncludeAllStr]))),
            new ContentInclude.Group(Condition: ReleaseConfigCondition, Content:
                [
                                             new ContentInclude(include: CreateIncludePath(args: [ResourceManager.ResourcesSubfolder, IncludeAllStr]),
                                                                linkDirectory: ResourceManager.ResourcesSubfolder,
                                                                exclude: ExcludeFoldersFromOutput),
                                             new ContentInclude(include: string.Format(format: FileIncludeFmt, arg0: SymbolPackage.SymbolExtension),
                                                                linkDirectory: SymbolPackage.SymbolsSubfolder,
                                                                exclude: ExcludeFoldersFromOutput),
                                             new ContentInclude(include: string.Format(format: FileIncludeFmt, arg0: EditorSymbolPackage.SymbolUiExtension),
                                                                linkDirectory: EditorSymbolPackage.SymbolUiSubFolder,
                                                                exclude: ExcludeFoldersFromOutput),
                                             new ContentInclude(include: string.Format(format: FileIncludeFmt, arg0: EditorSymbolPackage.SourceCodeExtension),
                                                                linkDirectory: EditorSymbolPackage.SourceCodeSubFolder,
                                                                exclude: ExcludeFoldersFromOutput)
                                         ])
        ];
    
    private static string CreateIncludePath(params string[] args) => string.Join(separator: ResourceManager.PathSeparator, value: args);


    private readonly record struct Using(string Name, string? Alias = null, bool Static = false);

    // Goal: No operator should need a Using statement for a slot type or operator duplication
    private static readonly Using[] DefaultUsingStatements =
        [
            // default System includes
            new (Name: "System"),
            new (Name: "System.Numerics"),
            new (Name: "System.Linq"),
            new (Name: "System.Linq.Enumerable", Static: true),
            new (Name: "System.Collections"),
            new (Name: "System.Linq.Expressions"),
            new (Name: "System.Collections.Generic"),
            new (Name: "System.Text"),
            new (Name: "System.Net"),
            new (Name: "System.Net.Http"),
            new (Name: "System.Threading.Tasks"),
            new (Name: "System.IO"),
            
            // T3 convenience includes
            new (Name: "T3.Core.Logging"),
            new (Name: "System.Runtime.InteropServices"),
            new (Name: "T3.Core.Operator"),
            new (Name: "T3.Core.Operator.Attributes"),
            new (Name: "T3.Core.Operator.Slots"),
            new (Name: "T3.Core.DataTypes"),
            new (Name: "T3.Core.Operator.Interfaces"),
            new (Name: "T3.Core.Resource"),
            
            // SharpDX types
            new (Name: "SharpDX.Direct3D11.Buffer", Alias: "Buffer"),
            new (Name: "SharpDX.Direct3D11.ShaderResourceView", Alias: "ShaderResourceView"),
            new (Name: "SharpDX.Direct3D11.UnorderedAccessView", Alias: "UnorderedAccessView"),
            new (Name: "SharpDX.Direct3D11.CullMode", Alias: "CullMode"),
            new (Name: "SharpDX.Direct3D11.FillMode", Alias: "FillMode"),
            new (Name: "SharpDX.Direct3D11.TextureAddressMode", Alias: "TextureAddressMode"),
            new (Name: "SharpDX.Direct3D11.Filter", Alias: "Filter"),
            new (Name: "SharpDX.DXGI.Format", Alias: "Format"),
            new (Name: "SharpDX.Direct3D11.Texture2DDescription", Alias: "Texture2DDescription"),
            new (Name: "SharpDX.Direct3D11.Texture3DDescription", Alias: "Texture3DDescription"),
            new (Name: "SharpDX.Direct3D11.RenderTargetBlendDescription", Alias: "RenderTargetBlendDescription"),
            new (Name: "SharpDX.Direct3D11.SamplerState", Alias: "SamplerState"),
            new (Name: "SharpDX.Direct3D11.UnorderedAccessViewBufferFlags", Alias: "UnorderedAccessViewBufferFlags"),
            new (Name: "SharpDX.Mathematics.Interop.RawRectangle", Alias: "RawRectangle"),
            new (Name: "SharpDX.Mathematics.Interop.RawViewportF", Alias: "RawViewportF"),
            new (Name: "SharpDX.Direct3D11.ResourceUsage", Alias: "ResourceUsage"),
            new (Name: "SharpDX.Direct3D11.ResourceOptionFlags", Alias: "ResourceOptionFlags"),
            new (Name: "SharpDX.Direct3D11.InputLayout", Alias: "InputLayout"),
            new (Name: "SharpDX.Direct3D.PrimitiveTopology", Alias: "PrimitiveTopology"),
            new (Name: "SharpDX.Direct3D11.BlendState", Alias: "BlendState"),
            new (Name: "SharpDX.Direct3D11.Comparison", Alias: "Comparison"),
            new (Name: "SharpDX.Direct3D11.BlendOption", Alias: "BlendOption"),
            new (Name: "SharpDX.Direct3D11.BlendOperation", Alias: "BlendOperation"),
            new (Name: "SharpDX.Direct3D11.BindFlags", Alias: "BindFlags"),
            new (Name: "SharpDX.Direct3D11.ColorWriteMaskFlags", Alias: "ColorWriteMaskFlags"),
            new (Name: "SharpDX.Direct3D11.CpuAccessFlags", Alias: "CpuAccessFlags"),
            new (Name: "SharpDX.Direct3D11.DepthStencilView", Alias: "DepthStencilView"),
            new (Name: "SharpDX.Direct3D11.DepthStencilState", Alias: "DepthStencilState"),
            new (Name: "SharpDX.Direct3D11.RenderTargetView", Alias: "RenderTargetView"),
            new (Name: "SharpDX.Direct3D11.RasterizerState", Alias: "RasterizerState"),
            
            
            // T3 types
            new (Name: "T3.Core.DataTypes.Point", Alias: "Point"),
            new (Name: "T3.Core.DataTypes.Texture2D", Alias: "Texture2D"),
            new (Name: "T3.Core.DataTypes.Texture3D", Alias: "Texture3D"),
            new (Name: "System.Numerics.Vector2", Alias: "Vector2"),
            new (Name: "System.Numerics.Vector3", Alias: "Vector3"),
            new (Name: "System.Numerics.Vector4", Alias: "Vector4"),
            new (Name: "System.Numerics.Matrix4x4", Alias: "Matrix4x4"),
            new (Name: "System.Numerics.Quaternion", Alias: "Quaternion"),
            new (Name: "T3.Core.DataTypes.Vector.Int2", Alias: "Int2"),
            new (Name: "T3.Core.DataTypes.Vector.Int3", Alias: "Int3"),
            new (Name: "T3.Core.DataTypes.Vector.Int4", Alias: "Int4"),
            new (Name: "T3.Core.Resource.ResourceManager", Alias: "ResourceManager"),
            new (Name: "T3.Core.DataTypes.ComputeShader", Alias: "ComputeShader"),
            new (Name: "T3.Core.DataTypes.PixelShader", Alias: "PixelShader"),
            new (Name: "T3.Core.DataTypes.VertexShader", Alias: "VertexShader"),
            new (Name: "T3.Core.DataTypes.GeometryShader", Alias: "GeometryShader")
        ];
}