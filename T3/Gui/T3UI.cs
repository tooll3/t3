using ImGuiNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Animation;
using T3.Gui.Commands;
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
            OpenNewParameterView();
            _quickCreateWindow = new QuickCreateWindow();
        }

        public static void OpenNewGraphWindow()
        {
            _instance._graphCanvasWindows.Add(new GraphCanvasWindow(_uiModel.MainOp, "Composition View " + _instance._graphCanvasWindows.Count));
        }

        public static void OpenNewParameterView()
        {
            _instance._parameterWindows.Add(new ParameterWindow("Parameter View " + _instance._parameterWindows.Count));
        }


        public unsafe void DrawUI()
        {
            DrawGraphCanvasWindows();
            DrawGraphParameterWindows();

            if (UiSettingsWindow.DemoWindowVisible)
                ImGui.ShowDemoWindow(ref UiSettingsWindow.DemoWindowVisible);

            if (UiSettingsWindow.ShowMetrics)
                ImGui.ShowMetricsWindow(ref UiSettingsWindow.ShowMetrics);


            if (UiSettingsWindow.ConsoleWindowVisible)
                _consoleWindow.Draw(ref UiSettingsWindow.ConsoleWindowVisible);

            if (UiSettingsWindow.CurveEditorVisible)
                _curveEditor.Draw(ref UiSettingsWindow.CurveEditorVisible);

            _quickCreateWindow.Draw();
            SwapHoveringBuffers();
        }

        private SymbolChildUi GetSelectedSymbolChildUi()
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


        private unsafe void DrawGraphParameterWindows()
        {
            ParameterWindow obsoleteWindow = null;
            foreach (var g in _parameterWindows)
            {
                Instance op = null;

                var symbolChildUi = GetSelectedSymbolChildUi();
                if (symbolChildUi != null)
                {
                    op = _graphCanvasWindows[0].Canvas.CompositionOp.Children.SingleOrDefault(child => child.Id == symbolChildUi.Id);
                }

                if (!g.Draw(op))
                    obsoleteWindow = g;   // we assume that only one window can be close in per frame
            }
            if (obsoleteWindow != null)
                _parameterWindows.Remove(obsoleteWindow);
        }


        public void DrawSelectedWindow()
        {
            ImGui.Begin("SelectionView");
            if (_instance._graphCanvasWindows.Any())
            {
                Instance selectedInstance = _instance._graphCanvasWindows[0].Canvas.CompositionOp; // todo: fix
                SymbolUi selectedUi = SymbolUiRegistry.Entries[selectedInstance.Symbol.Id];
                var selectedChildUi = selectedUi.ChildUis.FirstOrDefault(childUi => childUi.IsSelected);
                if (selectedChildUi != null)
                {
                    selectedInstance = selectedInstance.Children.Single(child => child.Id == selectedChildUi.Id);
                    selectedUi = SymbolUiRegistry.Entries[selectedInstance.Symbol.Id];
                }

                if (selectedInstance.Outputs.Count > 0)
                {
                    var firstOutput = selectedInstance.Outputs[0];
                    IOutputUi outputUi = selectedUi.OutputUis[firstOutput.Id];
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
        private List<ParameterWindow> _parameterWindows = new List<ParameterWindow>();

        public static UiModel _uiModel = new UiModel();
        private ConsoleLogWindow _consoleWindow = new ConsoleLogWindow();
        private CurveEditorWindow _curveEditor = new CurveEditorWindow();
        private static T3UI _instance = null;
        private QuickCreateWindow _quickCreateWindow = null;
    }
}
