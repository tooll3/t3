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
using T3.Gui.Animation.CurveEditing;
using T3.Gui.Commands;
using T3.Gui.Graph;
using T3.Gui.OutputUi;
using T3.Gui.Windows;

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

            _windows = new List<Window>()
            {
                new SettingsWindow(),
                new QuickCreateWindow(),
                new ConsoleLogWindow(),
                new GraphCanvasWindow(),
                new OutputWindow(),
            };

            // Open a default Window
            //OpenNewGraphWindow();
            OpenNewParameterView();
        }



        //public static void OpenNewGraphWindow()
        //{
        //    _instance._graphCanvasWindows.Add(new GraphCanvasWindow(UiModel.MainOp, "Composition View " + _instance._graphCanvasWindows.Count));
        //}

        public static void OpenNewParameterView()
        {
            _instance._parameterWindows.Add(new ParameterWindow("Parameter View " + _instance._parameterWindows.Count));
        }


        public unsafe void DrawUI()
        {
            DrawAppMenu();

            foreach (var windowType in _windows)
            {
                windowType.Draw();
            }

            DrawGraphParameterWindows();
            //DrawSelectionWindow();

            if (SettingsWindow.DemoWindowVisible)
                ImGui.ShowDemoWindow(ref SettingsWindow.DemoWindowVisible);

            if (SettingsWindow.ShowMetrics)
                ImGui.ShowMetricsWindow(ref SettingsWindow.ShowMetrics);


            SwapHoveringBuffers();
        }

        private void DrawAppMenu()
        {

            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Save"))
                    {
                        UiModel.Save();
                    }
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Edit"))
                {
                    if (ImGui.MenuItem("Undo", "CTRL+Z", false, UndoRedoStack.CanUndo))
                    {
                        UndoRedoStack.Undo();
                    }

                    if (ImGui.MenuItem("Redo", "CTRL+Y", false, UndoRedoStack.CanRedo))
                    {
                        UndoRedoStack.Redo();
                    }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Cut", "CTRL+X")) { }
                    if (ImGui.MenuItem("Copy", "CTRL+C")) { }
                    if (ImGui.MenuItem("Paste", "CTRL+V")) { }
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Windows"))
                {
                    foreach (var window in _windows)
                    {
                        window.DrawMenuItemToggle();
                    }
                    //SettingsWindow.DrawMenuItemToggle();
                    ImGui.EndMenu();
                }
                ImGui.EndMainMenuBar();
            }
        }

        private SymbolChildUi GetSelectedSymbolChildUi()
        {
            foreach (var gcw in GraphCanvasWindow.WindowInstances)
            {
                if (gcw.Canvas.SelectionHandler.SelectedElements.Any())
                {
                    var ui = gcw.Canvas.SelectionHandler.SelectedElements[0] as SymbolChildUi;
                    if (ui != null)
                        return ui;
                }
            }
            return null;
        }


        private IOutputUi GetSelectedOutputUi()
        {
            foreach (var gcw in GraphCanvasWindow.WindowInstances)
            {
                if (gcw.Canvas.SelectionHandler.SelectedElements.Any())
                {
                    var outputUi = gcw.Canvas.SelectionHandler.SelectedElements[0] as IOutputUi;
                    if (outputUi != null)
                        return outputUi;
                }
            }
            return null;
        }





        private unsafe void DrawGraphParameterWindows()
        {
            ParameterWindow obsoleteWindow = null;
            Instance instance = null;

            var symbolChildUi = GetSelectedSymbolChildUi();
            if (symbolChildUi != null)
            {
                instance = GraphCanvasWindow.WindowInstances[0].Canvas.CompositionOp.Children.SingleOrDefault(
                    child => child.Id == symbolChildUi.Id);
            }

            foreach (var parameterView in _parameterWindows)
            {
                if (!parameterView.Draw(instance, symbolChildUi))
                    obsoleteWindow = parameterView;   // we assume that only one window can be closed per frame
            }
            if (obsoleteWindow != null)
                _parameterWindows.Remove(obsoleteWindow);
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

        //private List<GraphCanvasWindow> _graphCanvasWindows = new List<GraphCanvasWindow>();
        private List<ParameterWindow> _parameterWindows = new List<ParameterWindow>();

        public static UiModel UiModel = new UiModel();

        private static T3UI _instance = null;
        //private QuickCreateWindow _quickCreateWindow = null;

        private List<Window> _windows;
    }
}
