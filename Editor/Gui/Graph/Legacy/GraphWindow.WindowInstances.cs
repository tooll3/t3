#nullable enable
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Graph.GraphUiModel;
using T3.Editor.Gui.Graph.Legacy.Interaction;
using T3.Editor.Gui.Graph.Legacy.Interaction.Connections;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.Gui.Windows.Layouts;
using T3.Editor.UiModel;
using T3.Editor.UiModel.ProjectSession;

namespace T3.Editor.Gui.Graph.Legacy;

/// <summary>
/// Features related to create a GraphWindow with a <see cref="EditableSymbolProject"/>.
/// After loading the project it will initialize the window's <see cref="GraphComponents"/>
/// and handle tear down if the window is closed.
/// </summary>
internal sealed partial class GraphWindow
{
    public static GraphWindow? Focused
    {
        get => _focused;
        private set
        {
            if (_focused == value)
                return;

            _focused?.OnFocusLost?.Invoke(_focused, _focused);
            _focused = value;
        }
    }

    internal event EventHandler<GraphWindow>? OnFocusLost;
    private readonly int _instanceNumber;

    private GraphWindow(int instanceNumber, GraphComponents components)
    {
        Components = components;
        _instanceNumber = instanceNumber;
        WindowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        AllowMultipleInstances = true;
        components.SymbolBrowser.OnFocusRequested += FocusFromSymbolBrowserHandler;
        OnFocusLost += (_, _) =>
                     {
                         var nodeSelection = components.NodeSelection;
                         nodeSelection.Clear();
                         nodeSelection.HoveredIds.Clear();
                     };

        OnWindowDestroyed += (_, _) =>
                           {
                               components.GraphCanvas.Destroyed = true;
                           };
        
        components.OnCompositionChanged += CompositionChangedHandler;

        ConnectionMaker.AddWindow(components.GraphCanvas);
        WindowDisplayTitle = components.OpenedProject.Package.DisplayName + "##" + _instanceNumber;
        SetWindowToNormal();
    }

    // TODO: this might not work because the deconstructor is only called by the GC later. GraphWindow should be IDisposable 
    ~GraphWindow()
    {
        Components.OnCompositionChanged -= CompositionChangedHandler;
    }

    private void CompositionChangedHandler(GraphComponents _, Guid instanceId)
    {
        UserSettings.SaveLastViewedOpForWindow(Config.Title, instanceId);
    }

    public GraphComponents Components { get; }
    public override IReadOnlyList<Window> GetInstances() => GraphWindowInstances;
    public event EventHandler<EditorSymbolPackage>? OnWindowDestroyed;

    
    public static bool CanOpenAnotherWindow => true;
    internal static readonly List<GraphWindow> GraphWindowInstances = [];
    
    public static bool TryOpenPackage(EditorSymbolPackage package, bool replaceFocused, Instance? startingComposition = null, WindowConfig? config = null,
                                      int instanceNumber = NoInstanceNumber)
    {
        if (!package.TryGetRootInstance(out var root))
        {
            LogFailure();
            return false;
        }

        if(!OpenedProject.TryCreate(package, out var openedProject))
        {
            LogFailure();
            return false;
        }
        
        var shouldBeVisible = true;
        if (replaceFocused && Focused != null)
        {
            shouldBeVisible = Focused.Config.Visible;
            Focused.Close();
        }
        
        instanceNumber = instanceNumber == NoInstanceNumber ? ++_instanceCounter : instanceNumber;
        
        // check for existing OpenedProject object, if it doesnt exist then create one 
        var components = CreateComponentsForLegacyGraphCanvas(openedProject);
        var newWindow = new GraphWindow(instanceNumber, components);

        if (config == null)
        {
            config = newWindow.Config;
            config.Title = LayoutHandling.GraphPrefix + instanceNumber;
            config.Visible = shouldBeVisible;
        }
        else
        {
            config.Visible = true;
            newWindow.Config = config;
        }

        IReadOnlyList<Guid> rootPath = [root.SymbolChildId];
        var startPath = rootPath;
        if (root == startingComposition)
        {
            // Legacy work-around
            var opId = UserSettings.GetLastOpenOpForWindow(config.Title);
            if (opId != Guid.Empty && opId != root.SymbolChildId)
            {
                if (root.TryGetChildInstance(opId, true, out _, out var path))
                {
                    startPath = path;
                }
            }
        }

        const ICanvas.Transition transition = ICanvas.Transition.JumpIn;
        if (!components.TrySetCompositionOp(startPath, transition) && !components.TrySetCompositionOp(rootPath, transition))
        {
            LogFailure();
            newWindow.Close();
            return false;
        }

        GraphWindowInstances.Add(newWindow);
        newWindow.TakeFocus();
        return true;

        void LogFailure() => Log.Error($"Failed to open operator graph for {package.DisplayName}");
    }

    public static GraphComponents CreateComponentsForLegacyGraphCanvas(OpenedProject openedProject)
    {
        GraphComponents.CreateIndependentComponents(openedProject, out var navigationHistory, out var nodeSelection, out var graphImageBackground);
        var components = new GraphComponents(openedProject, navigationHistory, nodeSelection, graphImageBackground);
        var canvas = new GraphCanvas(nodeSelection, openedProject.Structure, navigationHistory, components.NodeNavigation,
                                     getComposition: () => components.CompositionOp)
                         {
                             Components = components
                         };

        components._graphCanvas = canvas;
        components.SymbolBrowser = new SymbolBrowser(components, canvas);
        return components;
    }

    
    public void SetWindowToNormal()
    {
        WindowFlags &= ~(ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoMove |
                         ImGuiWindowFlags.NoResize);
    }

    private void TakeFocus()
    {
        Focused = this;
    }

    private static GraphWindow? _focused;
    private static int _instanceCounter;
    private const int NoInstanceNumber = -1;
}