namespace T3.Core.Resource;

/// <summary>
/// The interface for projects that want to share resources
/// If a project implements this interface, and ShouldShareResources is true,
/// that project's resources will be accessible to all other projects, regardless of the operator hierarchy.
/// See <see cref="ResourceManager.SharedResourcePackages"/>
/// </summary>
public interface IShareResources
{
    public bool ShouldShareResources { get; }
}