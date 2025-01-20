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
    
    private Instance? _rootInstance;
    public Instance RootInstance
    {
        get
        {
            if (_rootInstance != null) return _rootInstance;
            
            var rootSymbolId = Package.HomeSymbolId;
            if (!Package.Symbols.TryGetValue(rootSymbolId, out var rootSymbol))
            {
                throw new Exception("Root symbol not found in project.");
            }

            if (!rootSymbol.TryGetParentlessInstance(out var rootInstance))
            {
                throw new Exception("Root instance could not be created?");
            }
                
            _rootInstance = rootInstance;
            rootInstance.Disposing += OnRootDisposed;

            return _rootInstance;
        }
    }

    private void OnRootDisposed()
    {
        _rootInstance!.Disposing -= OnRootDisposed;
        _rootInstance = null;
    }

    public static readonly Dictionary<EditorSymbolPackage, OpenedProject> OpenedProjects = new();

    public static bool TryCreate(EditorSymbolPackage project, [NotNullWhen(true)] out OpenedProject? openedProject)
    {
        if (OpenedProjects.TryGetValue(project, out openedProject))
        {
            return true;
        }
        
        if (!project.HasHome)
        {
            openedProject = null;
            return false;
        }

        openedProject = new OpenedProject(project);
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

    private OpenedProject(EditorSymbolPackage project)
    {
        Package = project;
        Structure = new Structure(() => RootInstance.SymbolChildId, () => Package.HomeSymbolId, project);
    }
}