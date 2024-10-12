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
            if (value != null)
                Log.Debug($"Focused! {value.Config.Title} + {value.InstanceNumber}");
        }
    }

    internal event EventHandler<GraphWindow>? FocusLost;
    public readonly int InstanceNumber;

    private GraphWindow(EditorSymbolPackage package, int instanceNumber, Instance rootInstance)
    {
        InstanceNumber = instanceNumber;
        WindowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        Package = package;

        RootInstance = Composition.GetFor(rootInstance!);
        AllowMultipleInstances = true;

        ConnectionMaker.AddWindow(this);
        Structure = new Structure(() => RootInstance.Instance);

        var navigationHistory = new NavigationHistory(Structure);
        var nodeSelection = new NodeSelection(navigationHistory, Structure);

        GraphImageBackground = new GraphImageBackground(this, nodeSelection, Structure);
        GraphCanvas = new GraphCanvas(this, nodeSelection, navigationHistory);
        SymbolBrowser = new SymbolBrowser(this, GraphCanvas);
        _timeLineCanvas = new TimeLineCanvas(GraphCanvas);
        WindowDisplayTitle = package.DisplayName + "##" + InstanceNumber;
        SetWindowToNormal();
        _duplicateSymbolDialog.Closed += DisposeLatestComposition;
    }

    public static bool CanOpenAnotherWindow => true;
    internal static readonly List<GraphWindow> GraphWindowInstances = new();
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

        instanceNumber = instanceNumber == NoInstanceNumber ? ++_instanceCounter : instanceNumber;
        var newWindow = new GraphWindow(package, instanceNumber, root);

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
        if (!newWindow.TrySetCompositionOp(startPath, transition) && !newWindow.TrySetCompositionOp(rootPath, transition))
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