#nullable enable
using Microsoft.Build.Construction;

namespace T3.Editor.Compilation;

internal sealed partial class CsProjectFile
{
    private static string EvaluatedVariable(string variableName) => $"$({variableName})";

    private static ProjectRootElement CreateNewProjectRootElement(string projectNamespace, Guid projectGuid)
    {
        var rootElement = ProjectRootElement.Create();
        rootElement.Sdk = "Microsoft.NET.Sdk";
        AddDefaultPropertyGroup(rootElement, projectNamespace, projectGuid);
        AddDefaultReferenceGroup(rootElement);
        AddDefaultContent(rootElement);
        return rootElement;
    }

    private static void AddDefaultPropertyGroup(ProjectRootElement project, string projectNamespace, Guid projectGuid)
    {
        var propertyGroup = project.AddPropertyGroup();
        foreach (var defaultProperty in DefaultProperties)
        {
            if (defaultProperty.PropertyType is PropertyType.ProjectGuid or PropertyType.RootNamespace)
            {
                Log.Warning($"Cannot set {defaultProperty.PropertyType} here - remove it from defaults\n" + Environment.StackTrace);
                continue;
            }
            
            var propertyName = PropertyTypeNames[defaultProperty.PropertyType];
            propertyGroup.AddProperty(propertyName, defaultProperty.Value);
        }
        
        propertyGroup.AddProperty(PropertyTypeNames[PropertyType.RootNamespace], projectNamespace);
        propertyGroup.AddProperty(PropertyTypeNames[PropertyType.ProjectGuid], projectGuid.ToString());
    }

    private static void AddDefaultReferenceGroup(ProjectRootElement project)
    {
        var itemGroup = project.AddItemGroup();
        foreach (var reference in DefaultReferences)
        {
            var item = itemGroup.AddItem(ItemTypeNames[reference.Type], reference.Include);
            foreach (var tag in reference.Tags)
            {
                item.AddMetadata(MetadataTagTypeNames[tag.Tag], tag.Value, tag.AddAsAttribute);
            }
        }
    }

    private static void AddDefaultContent(ProjectRootElement project)
    {
        var contentTagName = ItemTypeNames[ItemType.Content];
        
        foreach (var group in DefaultContent)
        {
            var itemGroup = project.AddItemGroup();
            itemGroup.Condition = group.Condition.ToString();
            foreach (var content in group.Content)
            {
                var item = itemGroup.AddItem(contentTagName, content.Include);
                if(content.Exclude != null)
                    item.Exclude = content.Exclude;
                
                foreach (var tag in content.GetTags())
                {
                    var name = MetadataTagTypeNames[tag.Tag];
                    item.AddMetadata(name, tag.Value, tag.AddAsAttribute);
                }
            }
            
        }
    }
}