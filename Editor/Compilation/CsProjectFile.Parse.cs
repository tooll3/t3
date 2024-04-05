using Microsoft.Build.Construction;

namespace T3.Editor.Compilation;

internal partial class CsProjectFile
{
    private static string GetProperty(PropertyType propertyType, ProjectRootElement project, string? defaultValue = null)
    {
        var properties = project.Properties;
        var propertyName = PropertyTypeNames[propertyType];
        var property = properties.SingleOrDefault(x => x.Name == propertyName);

        if (property == null)
        {
            if(properties.Any(x => x.Name == propertyName))
                throw new Exception($"Multiple properties with the same name: {propertyName}");

            defaultValue ??= DefaultProperties.First(x => x.PropertyType == propertyType).Value;
            
            property = SetOrAddProperty(propertyType, defaultValue, project);
        }
        
        return property.Value;
    }

    private static ProjectPropertyElement SetOrAddProperty(PropertyType propertyType, string value, ProjectRootElement project)
    {
        var properties = project.Properties;
        var propertyName = PropertyTypeNames[propertyType];
        var property = properties.SingleOrDefault(x => x.Name == propertyName);

        if (property == null)
        {
            if(properties.Any(x => x.Name == propertyName))
                throw new Exception($"Multiple properties with the same name: {propertyName}");
            
            property = project.AddProperty(propertyName, value);
        }
        else
        {
            property.Value = value;
        }

        return property;
    }
}