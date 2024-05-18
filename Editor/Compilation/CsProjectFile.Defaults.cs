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
                }
        .ToFrozenDictionary(x => x.Type, x => x.Value);
    
    private static readonly TagValue PrivateEnabledTag = new(MetadataTagType.Private, "true", true);
    private static readonly Reference[] DefaultReferences =
        [
            new Reference(ItemType.EditorReference, "Core.dll", PrivateEnabledTag),
            new Reference(ItemType.EditorReference, "Logging.dll", PrivateEnabledTag),
            new Reference(ItemType.EditorReference, "SharpDX.dll", PrivateEnabledTag),
            new Reference(ItemType.EditorReference, "SharpDX.Direct3D11.dll", PrivateEnabledTag),
            new Reference(ItemType.EditorReference, "SharpDX.DXGI.dll", PrivateEnabledTag),
            new Reference(ItemType.EditorReference, "SharpDX.Direct2D1.dll", PrivateEnabledTag),
        ];

    // Note : we are trying to stay platform-agnostic with directories, and so we use unix path separators
    private static readonly Condition ReleaseConfigCondition = new("Configuration", "Release", true);
    private const string IncludeAllStr = "**";
    private static readonly string[] ExcludeFoldersFromOutput = [CreateIncludePath("bin", IncludeAllStr), CreateIncludePath("obj", IncludeAllStr)];
    private const string FileIncludeFmt = IncludeAllStr + @"{0}";
    private static readonly ContentInclude.Group[] DefaultContent =
        [
            new ContentInclude.Group(null, new ContentInclude(CreateIncludePath(".", "dependencies", IncludeAllStr))),
            new ContentInclude.Group(ReleaseConfigCondition,
                                    new ContentInclude(include: CreateIncludePath(ResourceManager.ResourcesSubfolder,IncludeAllStr),
                                                       linkDirectory: ResourceManager.ResourcesSubfolder,
                                                       exclude: ExcludeFoldersFromOutput),
                                    new ContentInclude(include: string.Format(FileIncludeFmt, SymbolPackage.SymbolExtension),
                                                       linkDirectory: SymbolPackage.SymbolsSubfolder,
                                                       exclude: ExcludeFoldersFromOutput),
                                    new ContentInclude(include: string.Format(FileIncludeFmt, EditorSymbolPackage.SymbolUiExtension),
                                                       linkDirectory: EditorSymbolPackage.SymbolUiSubFolder,
                                                       exclude: ExcludeFoldersFromOutput),
                                    new ContentInclude(include: string.Format(FileIncludeFmt, EditorSymbolPackage.SourceCodeExtension),
                                                       linkDirectory: EditorSymbolPackage.SourceCodeSubFolder,
                                                       exclude: ExcludeFoldersFromOutput))
        ];
    
    private static string CreateIncludePath(params string[] args) => string.Join(ResourceManager.PathSeparator, args);
}