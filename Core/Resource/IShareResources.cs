namespace T3.Core.Resource;

/// <summary>
/// The interface for projects that want to share resources
/// Todo: This should be added to the project creation process with a boolean option.
/// If a project implements this interface, and ShouldShareResources is true,
/// that project's resources will be accessible to all other projects, regardless of the operator hierarchy.
/// See <see cref="ResourceManager.SharedResourceFolders"/>
/// </summary>
public interface IShareResources
{
    public bool ShouldShareResources { get; }
}