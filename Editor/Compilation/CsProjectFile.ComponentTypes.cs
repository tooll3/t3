#nullable enable
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using T3.Core.Compilation;

namespace T3.Editor.Compilation;

internal sealed partial class CsProjectFile
{
    #region CsProjectFile Xml Tag Types
    private enum PropertyType
    {
        RootNamespace,
        TargetFramework,
        DisableTransitiveProjectReferences,
        VersionPrefix,
        Nullable,
        HomeGuid,
        EditorVersion,
        AssemblyName,
        IsEditorOnly
    }

    private enum ItemType
    {
        ProjectReference,
        PackageReference,
        DllReference,
        /// <summary>
        /// Not technically a type seen in MSBuild, but used in T3 to refer to assemblies references from the t3 editor at runtime
        /// using <see cref="RuntimeAssemblies.EnvironmentVariableName"/>. See <see cref="CsProjectFile.ItemTypeNames"/> below.
        /// </summary>
        EditorReference,
        OperatorPackage,
        Content
    }

    private enum MetadataTagType
    {
        Include,
        Exclude,
        Link,
        CopyToOutputDirectory,
        Private,
        Label,
    }
    
    /// <summary>
    /// We need this collection to logically differentiate between standard Dll references and editor dll references
    /// </summary>
    private static readonly FrozenDictionary<ItemType, string> ItemTypeNames = new[]
            {
                (Name: nameof(ItemType.Content), Type: ItemType.Content),
                (Name: nameof(ItemType.ProjectReference), Type: ItemType.ProjectReference),
                (Name: nameof(ItemType.PackageReference), Type: ItemType.PackageReference),
                (Name: "Reference", Type: ItemType.DllReference),
                (Name: "Reference", Type: ItemType.EditorReference),
                (Name: "Operators", Type: ItemType.OperatorPackage)
            }
       .ToFrozenDictionary(x => x.Type, x => x.Name);
    
    // we implement this to keep the method of getting xml element names consistent. Avoids direct "ToString" calls to avoid ambiguity and bugs between
    // getting the name of each type of enum correctly
    private static string GetNameOf(ItemType itemType) => ItemTypeNames[itemType];
    private static string GetNameOf(MetadataTagType tagType) => tagType.ToString();
    private static string GetNameOf(PropertyType propertyType) => propertyType.ToString();
    
    #endregion CsProjectFile Xml Tag Types

    #region Utility Types for consistent handling
    private readonly record struct TagValue(MetadataTagType Tag, string Value, bool AddAsAttribute);

    private readonly struct Reference(ItemType type, string include, params TagValue[]? tags)
    {
        public readonly ItemType Type = type;
        
        public readonly string Include = type == ItemType.EditorReference
                                             ? Path.Combine(UnevaluatedVariable(RuntimeAssemblies.EnvironmentVariableName), include)
                                             : include;
        
        public readonly TagValue[] Tags = tags ?? Array.Empty<TagValue>();
    }

    private readonly record struct Condition(string ConditionVarName, string RequiredValue, bool IfEqual)
    {
        public override string ToString() => $"'{UnevaluatedVariable(ConditionVarName)}' {(IfEqual ? "==" : "!=")} '{RequiredValue}'";
    }

    private readonly record struct ContentInclude
    {
        public ContentInclude(string include, string? linkDirectory = null, string copyMode = "PreserveNewest", params string?[]? exclude)
        {
            _include = new TagValue(MetadataTagType.Include, include, true);

            linkDirectory = linkDirectory == null ? "" : linkDirectory + '/';
            _link = new TagValue(MetadataTagType.Link, linkDirectory + @"%(RecursiveDir)%(Filename)%(Extension)", false);

            _exclude = exclude is { Length: > 0 }
                           ? new TagValue(MetadataTagType.Exclude, string.Join(";", exclude), true)
                           : null;

            _copyMode = new TagValue(MetadataTagType.CopyToOutputDirectory, copyMode, true);
        }

        public IEnumerable<TagValue> GetTags()
        {
            yield return _link;
            yield return _copyMode;
        }

        public bool TryGetExclude([NotNullWhen(true)] out string? exclude)
        {
            exclude = _exclude?.Value;
            return _exclude is not null;
        }

        public string Include => _include.Value;
        private readonly TagValue _include;
        private readonly TagValue? _exclude;
        private readonly TagValue _link;
        private readonly TagValue _copyMode;
        public readonly record struct Group(Condition? Condition, params ContentInclude[] Content);
    }
    
    #endregion Utility Types for consistent handling

}