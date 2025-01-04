#nullable enable
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.Gui.Windows.Layouts;
using T3.Editor.Gui.Windows.TimeLine;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph;

internal sealed partial class GraphWindow
{
    public static GraphWindow? Focused
    {
        get => _focused;
        private set
        {
            if (_focused == value)
                return;

            _focused?.FocusLost?.Invoke(_focused, _focused);
            _focused = value;
        }
    }

    internal event EventHandler<GraphWindow>? FocusLost;
    public readonly int InstanceNumber;

    private GraphWindow(int instanceNumber, GraphComponents components)
    {
        Components = components;
        InstanceNumber = instanceNumber;
        WindowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        AllowMultipleInstances = true;
        components.GraphCanvas.SymbolBrowser.FocusRequested += FocusFromSymbolBrowser;
        FocusLost += (_, _) =>
                     {
                         var nodeSelection = components.NodeSelection;
                         nodeSelection.Clear();
                         nodeSelection.HoveredIds.Clear();
                     };

        WindowDestroyed += (_, _) =>
                           {
                               components.GraphCanvas.Destroyed = true;
                           };
        
        components.CompositionChanged += OnCompositionChanged;

        ConnectionMaker.AddWindow(components.GraphCanvas);
        WindowDisplayTitle = components.OpenedProject.Package.DisplayName + "##" + InstanceNumber;
        SetWindowToNormal();
    }

    ~GraphWindow()
    {
        Components.CompositionChanged -= OnCompositionChanged;
    }

    private void OnCompositionChanged(GraphComponents _, Guid instanceId)
    {
        UserSettings.SaveLastViewedOpForWindow(Config.Title, instanceId);
    }

    public static bool CanOpenAnotherWindow => true;
    internal static readonly List<GraphWindow> GraphWindowInstances = new();
    public GraphComponents Components { get; }
    public override IReadOnlyList<Window> GetInstances() => GraphWindowInstances;
    public event EventHandler<EditorSymbolPackage> WindowDestroyed;

    public static bool TryOpenPackage(EditorSymbolPackage package, bool replaceFocused, Instance? startingComposition = null, WindowConfig? config = null,
                                      int instanceNumber = NoInstanceNumber)
    {
        if (!package.TryGetRootInstance(out var root))
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
        
        if(!OpenedProject.TryCreate(package, out var openedProject))
        {
            LogFailure();
            return false;
        }

        instanceNumber = instanceNumber == NoInstanceNumber ? ++_instanceCounter : instanceNumber;
        // check for existing OpenedProject object, if it doesnt exist then create one 
        var components = new GraphComponents(openedProject);
        components.GraphCanvas = new GraphCanvas(components);
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

    public void SetWindowToNormal()
    {
        WindowFlags &= ~(ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoMove |
                         ImGuiWindowFlags.NoResize);
    }

    public void TakeFocus()
    {
        Focused = this;
    }

    private static GraphWindow? _focused;
    private static int _instanceCounter;
    private const int NoInstanceNumber = -1;
}