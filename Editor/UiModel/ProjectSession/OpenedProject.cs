#nullable enable
using System.Diagnostics.CodeAnalysis;
using T3.Core.Operator;
using T3.Editor.Gui.Graph.GraphUiModel;

namespace T3.Editor.UiModel.ProjectSession;

internal sealed class OpenedProject
{
    public readonly EditorSymbolPackage Package;
    public readonly Structure Structure;
    
    // TODO: This is not updated or used?
    private readonly List<GraphComponents> _graphWindowsComponents = [];
    
    public Composition RootInstance { get; private set; }
    
    private static readonly Dictionary<EditorSymbolPackage, OpenedProject> OpenedProjects = new();

    public static bool TryCreate(EditorSymbolPackage project, [NotNullWhen(true)] out OpenedProject? openedProject)
    {
        if(OpenedProjects.TryGetValue(project, out openedProject))
            return true;
        
        if (!project.TryGetRootInstance(out var rootInstance))
        {
            openedProject = null;
            return false;
        }

        openedProject = new OpenedProject(project, rootInstance);
        return true;
    }

    private OpenedProject(EditorSymbolPackage project, Instance rootInstance)
    {
        Package = project;
        RootInstance = Composition.GetFor(rootInstance);
        Structure = new Structure(() => RootInstance.Instance);
    }

    public void RefreshRootInstance()
    {
        if (!Package.TryGetRootInstance(out var newRootInstance))
        {
            throw new Exception("Could not get root instance from package");
        }

        var previousRoot = RootInstance.Instance;
        if (newRootInstance == previousRoot)
            return;

        RootInstance.Dispose();

        // check if the root instance was a window's composition
        // if it was, it needs to be replaced
        foreach (var components in _graphWindowsComponents)
        {
            if (components.Composition?.Instance == previousRoot)
                continue;

            components.Composition = Composition.GetFor(newRootInstance);
        }

        RootInstance = Composition.GetFor(newRootInstance);
    }

}