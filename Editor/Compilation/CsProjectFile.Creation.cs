#nullable enable
using Microsoft.Build.Construction;
using T3.Core.Compilation;

namespace T3.Editor.Compilation;

internal sealed partial class CsProjectFile
{
    private static string UnevaluatedVariable(string variableName) => $"$({variableName})";
    private static string UnevaluatedIterator(string variableName) => $"@({variableName})";

    private static ProjectRootElement CreateNewProjectRootElement(string projectNamespace, Guid homeGuid)
    {
        var rootElement = ProjectRootElement.Create();
        rootElement.Sdk = "Microsoft.NET.Sdk";
        AddDefaultPropertyGroup(rootElement, projectNamespace, homeGuid);
        AddDefaultReferenceGroup(rootElement);
        AddDefaultContent(rootElement);
        AddOpPackageItemGroup(rootElement);
        AddPackageInfoTarget(rootElement);
        return rootElement;
    }

    private static void AddDefaultPropertyGroup(ProjectRootElement project, string projectNamespace, Guid homeGuid)
    {
        var propertyGroup = project.AddPropertyGroup();
        foreach (var defaultProperty in DefaultProperties)
        {
            var propertyName = GetNameOf(defaultProperty.Key);
            if (defaultProperty.Key is PropertyType.HomeGuid or PropertyType.RootNamespace)
            {
                Log.Warning($"Cannot set {propertyName} here - remove it from defaults\n" + Environment.StackTrace);
                continue;
            }
            
            propertyGroup.AddProperty(propertyName, defaultProperty.Value);
        }
        
        propertyGroup.AddProperty(GetNameOf(PropertyType.RootNamespace), projectNamespace);
        propertyGroup.AddProperty(GetNameOf(PropertyType.HomeGuid), homeGuid.ToString());
        propertyGroup.AddProperty(GetNameOf(PropertyType.AssemblyName), UnevaluatedVariable(GetNameOf(PropertyType.RootNamespace)));
    }

    private static void AddDefaultReferenceGroup(ProjectRootElement project)
    {
        var itemGroup = project.AddItemGroup();
        foreach (var reference in DefaultReferences)
        {
            var item = itemGroup.AddItem(GetNameOf(reference.Type), reference.Include);
            foreach (var tag in reference.Tags)
            {
                item.AddMetadata(GetNameOf(tag.Tag), tag.Value, tag.AddAsAttribute);
            }
        }
    }

    private static void AddDefaultContent(ProjectRootElement project)
    {
        var contentTagName = GetNameOf(ItemType.Content);
        
        foreach (var group in DefaultContent)
        {
            var itemGroup = project.AddItemGroup();
            itemGroup.Condition = group.Condition.ToString();
            foreach (var content in group.Content)
            {
                var item = itemGroup.AddItem(contentTagName, content.Include);
                if(content.TryGetExclude(out var exclude))
                    item.Exclude = exclude;
                
                foreach (var tag in content.GetTags())
                {
                    item.AddMetadata(GetNameOf(tag.Tag), tag.Value, tag.AddAsAttribute);
                }
            }
        }
    }

    private static void AddOpPackageItemGroup(ProjectRootElement project)
    {
        var group = project.AddItemGroup();
        group.Label = OpPackItemGroupLabel;
    }

    private static void AddOperatorPackageTo(ProjectRootElement project, string name, string @namespace, Version version, bool resourcesOnly)
    {
        // find item group
        var group = project.ItemGroups.FirstOrDefault(x => x.Label == OpPackItemGroupLabel);
        if (group == null)
        {
            group = project.AddItemGroup();
            group.Label = OpPackItemGroupLabel;
        }
        
        // add item
        var item = group.AddItem(GetNameOf(ItemType.OperatorPackage), name + '.' + @namespace);
        item.AddMetadata(nameof(OperatorPackageReference.Version), version.ToBasicVersionString(), true);
        item.AddMetadata(nameof(OperatorPackageReference.ResourcesOnly), resourcesOnly.ToString(), true);
    }

    private static void AddPackageInfoTarget(ProjectRootElement project)
    {
        var target = project.AddTarget("CreatePackageInfo");
        target.AfterTargets = "AfterBuild";
        
        const string perPackageInfoTagName = "OpPackageInfoJson";

        const string version = nameof(OperatorPackageReference.Version);
        const string resourcesOnly = nameof(OperatorPackageReference.ResourcesOnly);
        const string includeVar = nameof(OperatorPackageReference.Identity); // dont ask me, this is just how MSBuild works. I guess the "Include" tag is
                                                                             // basically the identity of the item?
        
        const string jsonStructure = $"\n\t\t{{\n" +
                                     $"\t\t\t\"{includeVar}\": \"%({includeVar})\",\n" +
                                     $"\t\t\t\"{version}\": \"%({version})\", \n" +
                                     $"\t\t\t\"{resourcesOnly}\": \"%({resourcesOnly})\"\n" +
                                     $"\t\t}}";
        
        const string opReferencesArray = "OperatorReferenceArray";
        const string jsonArrayIterator = $"@({OpPackIncludeTagName} -> '%({perPackageInfoTagName})', ',')";
        
        var itemGroup = target.AddItemGroup();
        itemGroup.Label = "Define json structure of referenced operator packages";
        var item = itemGroup.AddItem(OpPackIncludeTagName, UnevaluatedIterator(GetNameOf(ItemType.OperatorPackage)));
        item.AddMetadata(perPackageInfoTagName, jsonStructure, false);
        
        var propertyGroup = target.AddPropertyGroup();
        propertyGroup.AddProperty(opReferencesArray, jsonArrayIterator);
        
        const string fullJsonTagName = "OperatorPackageInfoJson";
        var homeGuidPropertyName = GetNameOf(PropertyType.HomeGuid);
        var rootNamespacePropertyName = GetNameOf(PropertyType.RootNamespace);
        var editorVersionPropertyName = GetNameOf(PropertyType.EditorVersion);
        propertyGroup.AddProperty(fullJsonTagName, "{\n" +
                                                $"\t\"HomeGuid\": \"{UnevaluatedVariable(homeGuidPropertyName)}\", \n" +
                                                $"\t\"RootNamespace\": \"{UnevaluatedVariable(rootNamespacePropertyName)}\",\n" +
                                                $"\t\"Version\": \"{UnevaluatedVariable(GetNameOf(PropertyType.VersionPrefix))}\",\n" +
                                                $"\t\"EditorVersion\": \"{UnevaluatedVariable(editorVersionPropertyName)}\",\n" +
                                                $"\t\"OperatorPackages\": [{UnevaluatedVariable(opReferencesArray)}\n\t]\n" +
                                                "}\n");
        
        const string outputPathVariable = "OutputPath"; // built-in variable to get the output path of the project
        
        var task = target.AddTask("WriteLinesToFile");
        task.SetParameter("File", UnevaluatedVariable(outputPathVariable) + '/' + RuntimeAssemblies.PackageInfoFileName);
        task.SetParameter("Lines", UnevaluatedVariable(fullJsonTagName));
        task.SetParameter("Overwrite", "True");
        task.SetParameter("Encoding", "UTF-8");
    }

    private const string OpPackIncludeTagName = "OpPacks";
    private const string OpPackItemGroupLabel = "OperatorPackages";
}