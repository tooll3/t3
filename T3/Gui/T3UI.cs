using ImGuiNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using T3.Core;
using T3.Core.Commands;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Animation;
using T3.Gui.Graph;
using T3.Logging;

namespace T3.Gui
{
    /// <summary>
    /// A singleton capsule T3 UI functionality from imgui-clutter in Program.cs
    /// </summary>
    public class T3UI
    {
        public T3UI()
        {
            _instance = this;

            // Open a default Window
            OpenNewGraphWindow();
            _quickCreateWindow = new QuickCreateWindow();
        }

        public static void OpenNewGraphWindow()
        {
            _instance._graphCanvasWindows.Add(new GraphCanvasWindow(_uiModel.MainOp, "Composition View " + _instance._graphCanvasWindows.Count));
        }


        public unsafe void DrawUI()
        {
            DrawGraphCanvasWindows();
            if (UiSettingsWindow.DemoWindowVisible)
                ImGui.ShowDemoWindow(ref UiSettingsWindow.DemoWindowVisible);

            if (UiSettingsWindow.ShowMetrics)
                ImGui.ShowMetricsWindow(ref UiSettingsWindow.ShowMetrics);


            if (UiSettingsWindow.ConsoleWindowVisible)
                _consoleWindow.Draw(ref UiSettingsWindow.ConsoleWindowVisible);

            if (UiSettingsWindow.CurveEditorVisible)
                _curveEditor.Draw(ref UiSettingsWindow.CurveEditorVisible);

            if (UiSettingsWindow.ParameterWindowVisible)
            {
                if (_graphCanvasWindows.Any())
                {
                    ParameterWindow.Draw(_graphCanvasWindows[0].Canvas.CompositionOp, GetInstanceSelectedInGraph());
                }
            }
            _quickCreateWindow.Draw();

            SwapHoveringBuffers();
        }

        private SymbolChildUi GetInstanceSelectedInGraph()
        {
            foreach (var gcw in _graphCanvasWindows)
            {
                if (gcw.Canvas.SelectionHandler.SelectedElements.Any())
                {
                    var ui = gcw.Canvas.SelectionHandler.SelectedElements[0] as SymbolChildUi;
                    return ui;
                }
            }
            return null;
        }


        private unsafe void DrawGraphCanvasWindows()
        {
            GraphCanvasWindow obsoleteGraphWindow = null;
            foreach (var g in _graphCanvasWindows)
            {
                if (!g.Draw())
                    obsoleteGraphWindow = g;   // we assume that only one window can be close in per frame
            }
            if (obsoleteGraphWindow != null)
                _graphCanvasWindows.Remove(obsoleteGraphWindow);
        }

        public void DrawSelectedOutput()
        {
            ImGui.Begin("SelectionView");
            if (_instance._graphCanvasWindows.Any())
            {
                var compositionOp = _instance._graphCanvasWindows[0].Canvas.CompositionOp; // todo: fix
                Instance selectedInstance = compositionOp;
                var childUiEntries = SymbolChildUiRegistry.Entries[compositionOp.Symbol.Id];
                var selectedChildUi = childUiEntries.FirstOrDefault(childUi => childUi.Value.IsSelected).Value;
                if (selectedChildUi != null)
                {
                    var symbolChild = selectedChildUi.SymbolChild;
                    selectedInstance = compositionOp.Children.Single(child => child.Id == symbolChild.Id);
                }

                if (selectedInstance.Outputs.Count > 0)
                {
                    var firstOutput = selectedInstance.Outputs[0];
                    IOutputUi outputUi = OutputUiRegistry.Entries[selectedInstance.Symbol.Id][firstOutput.Id];
                    outputUi.DrawValue(firstOutput);
                }
            }

            if (ImGui.Button("serialize"))
            {
                _uiModel.Save();
            }

            if (UndoRedoStack.CanUndo && ImGui.Button("undo"))
            {
                UndoRedoStack.Undo();
            }

            if (UndoRedoStack.CanRedo && ImGui.Button("redo"))
            {
                UndoRedoStack.Redo();
            }

            ImGui.End();
        }

        public static void AddHoveredId(Guid id)
        {
            _hoveredIdsForNextFrame.Add(id);
        }

        public void SwapHoveringBuffers()
        {
            HoveredIdsLastFrame = _hoveredIdsForNextFrame;
            _hoveredIdsForNextFrame = new HashSet<Guid>();
        }

        public static HashSet<Guid> _hoveredIdsForNextFrame = new HashSet<Guid>();
        public static HashSet<Guid> HoveredIdsLastFrame { get; set; } = new HashSet<Guid>();



        private List<GraphCanvasWindow> _graphCanvasWindows = new List<GraphCanvasWindow>();
        public static UiModel _uiModel = new UiModel();
        private ConsoleLogWindow _consoleWindow = new ConsoleLogWindow();
        private CurveEditorWindow _curveEditor = new CurveEditorWindow();
        private static T3UI _instance = null;
        private QuickCreateWindow _quickCreateWindow = null;
    }
}
