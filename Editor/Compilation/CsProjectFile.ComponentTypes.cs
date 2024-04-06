#nullable enable
using System.Collections.Frozen;
using System.IO;
using T3.Core.Compilation;

namespace T3.Editor.Compilation;

internal sealed partial class CsProjectFile
{
    private enum PropertyType
    {
        RootNamespace,
        TargetFramework,
        DisableTransitiveProjectReferences,
        VersionPrefix,
        Nullable,
        HomeGuid,
        EditorVersion
    }

    private enum ItemType
    {
        ProjectReference,
        PackageReference,
        DllReference,
        EditorReference,
        Content
    }

    private enum MetadataTagType
    {
        Include,
        Exclude,
        Link,
        CopyToOutputDirectory,
        Private
    }

    private readonly record struct TagValue(MetadataTagType Tag, string Value, bool AddAsAttribute);

    private readonly record struct Property(PropertyType PropertyType, string Value);

    private readonly record struct Reference
    {
        public Reference(ItemType Type, string Include, params TagValue[]? Tags)
        {
            this.Type = Type;
            this.Include = Type == ItemType.EditorReference
                               ? Path.Combine(EvaluatedVariable(RuntimeAssemblies.EnvironmentVariableName), Include)
                               : Include;
            this.Tags = Tags ?? Array.Empty<TagValue>();
        }

        public readonly ItemType Type;
        public readonly string Include;
        public readonly TagValue[] Tags;
    }

    private readonly record struct Condition(string ConditionVarName, string RequiredValue, bool Negated = false)
    {
        public override string ToString() => $"'{EvaluatedVariable(ConditionVarName)}' {(Negated ? "!=" : "==")} '{RequiredValue}'";
    }

    private readonly record struct ContentIncludeGroup(Condition? Condition, params ContentInclude[] Content);

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

        public string Include => _include.Value;
        private readonly TagValue _include;
        public string? Exclude => _exclude?.Value;
        private readonly TagValue? _exclude;
        private readonly TagValue _link;
        private readonly TagValue _copyMode;
    }

    private static readonly FrozenDictionary<PropertyType, string> PropertyTypeNames = new[]
            {
                (Name: "RootNamespace", Type: PropertyType.RootNamespace),
                (Name: "TargetFramework", Type: PropertyType.TargetFramework),
                (Name: "DisableTransitiveProjectReferences", Type: PropertyType.DisableTransitiveProjectReferences),
                (Name: "VersionPrefix", Type: PropertyType.VersionPrefix),
                (Name: "Nullable", Type: PropertyType.Nullable),
                (Name: "HomeGuid", Type: PropertyType.HomeGuid),
                (Name: "EditorVersion", Type: PropertyType.EditorVersion)
            }
       .ToFrozenDictionary(x => x.Type, x => x.Name);

    private static readonly FrozenDictionary<ItemType, string> ItemTypeNames = new[]
            {
                (Name: "ProjectReference", Type: ItemType.ProjectReference),
                (Name: "PackageReference", Type: ItemType.PackageReference),
                (Name: "Reference", Type: ItemType.DllReference),
                (Name: "Reference", Type: ItemType.EditorReference),
                (Name: "Content", Type: ItemType.Content),
            }
       .ToFrozenDictionary(x => x.Type, x => x.Name);

    private static readonly FrozenDictionary<MetadataTagType, string> MetadataTagTypeNames = new[]
            {
                (Name: "Link", Type: MetadataTagType.Link),
                (Name: "Include", Type: MetadataTagType.Include),
                (Name: "Exclude", Type: MetadataTagType.Exclude),
                (Name: "CopyToOutputDirectory", Type: MetadataTagType.CopyToOutputDirectory),
                (Name: "Private", Type: MetadataTagType.Private)
            }
       .ToFrozenDictionary(x => x.Type, x => x.Name);
}