#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using T3.Core.Operator;

namespace T3.Editor.UiModel.ProjectHandling;

internal sealed class OpenedProject
{
    public readonly EditorSymbolPackage Package;
    public readonly Structure Structure;
    
    // TODO: This is not updated or used?
    private readonly List<ProjectView> _projectViews = [];
    
    public Composition RootInstance { get; private set; }
    
    public static readonly Dictionary<EditorSymbolPackage, OpenedProject> OpenedProjects = new();

    public static bool TryCreate(EditorSymbolPackage project, [NotNullWhen(true)] out OpenedProject? openedProject)
    {
        if (OpenedProjects.TryGetValue(project, out openedProject))
        {
            return true;
        }
        
        if (!project.TryGetRootInstance(out var rootInstance))
        {
            openedProject = null;
            return false;
        }

        openedProject = new OpenedProject(project, rootInstance);
        OpenedProjects[openedProject.Package] = openedProject;
        return true;
    }

    internal void RegisterView(ProjectView view)
    {
        Debug.Assert(!_projectViews.Contains(view));
        _projectViews.Add(view);
    }

    internal void UnregisterView(ProjectView view)
    {
        Debug.Assert(_projectViews.Contains(view));
        _projectViews.Remove(view);
    }

    private OpenedProject(EditorSymbolPackage project, Instance rootInstance)
    {
        Package = project;
        RootInstance = Composition.GetForInstance(rootInstance);
        Structure = new Structure(() => RootInstance.Instance);
    }

    public void RefreshRootInstance(ProjectView projectView)
    {
        if (!Package.TryGetRootInstance(out var newRootInstance))
        {
            throw new Exception("Could not get root instance from package");
        }

        var previousRoot = RootInstance.Instance;
        if (newRootInstance == previousRoot)
            return;

        RootInstance.Dispose();

        // Check if the root instance was a window's composition.
        // If it was, it needs to be replaced
        foreach (var components in _projectViews)
        {
            if (components.Composition?.Instance == previousRoot)
                continue;

            components.Composition = Composition.GetForInstance(newRootInstance);
        }

        RootInstance = Composition.GetForInstance(newRootInstance);
    }

}