using T3.Core.Model;
using T3.Core.Resource;
using T3.Editor.UiModel;

namespace T3.Editor.Compilation;

internal sealed partial class CsProjectFile
{
    private static readonly Property[] DefaultProperties =
        [
            new Property(PropertyType.TargetFramework, "net8.0-windows"),
            new Property(PropertyType.DisableTransitiveProjectReferences, "true"),
            new Property(PropertyType.VersionPrefix, "1.0.0"),
            new Property(PropertyType.Nullable, "enable")
        ];
    
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

    private static readonly Condition ReleaseConfigCondition = new("Configuration", "Release");
    private static readonly ContentIncludeGroup[] DefaultContent =
        [
            new ContentIncludeGroup(null, new ContentInclude("./dependencies/**/*")),
            new ContentIncludeGroup(ReleaseConfigCondition,
                                    new ContentInclude(include: $"{ResourceManager.ResourcesSubfolder}/**/*",
                                                       linkDirectory: ResourceManager.ResourcesSubfolder,
                                                       exclude: "bin/**"),
                                    new ContentInclude(include: $"**/*{SymbolPackage.SymbolExtension}",
                                                       linkDirectory: SymbolPackage.SymbolsSubfolder,
                                                       exclude: "bin/**"),
                                    new ContentInclude(include: $"**/*{EditorSymbolPackage.SymbolUiExtension}",
                                                       linkDirectory: EditorSymbolPackage.SymbolUiSubFolder,
                                                       exclude: "bin/**"),
                                    new ContentInclude(include: $"**/*{EditorSymbolPackage.SourceCodeExtension}",
                                                       linkDirectory: EditorSymbolPackage.SourceCodeSubFolder,
                                                       exclude: new[] { "bin/**", "obj/**" })),
        ];
    
}