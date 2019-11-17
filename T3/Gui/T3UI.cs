using ImGuiNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Xml.Schema;
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
                new GraphWindow(),
                new ParameterWindow(),
                new OutputWindow(),
                new ConsoleLogWindow(),
                new SettingsWindow(),
                new QuickCreateWindow(),
            };
        }


        public unsafe void DrawUI()
        {

            foreach (var windowType in _windows)
            {
                windowType.Draw();
            }

            if (_demoWindowVisible)
                ImGui.ShowDemoWindow(ref _demoWindowVisible);

            if (_metricsWindowVisible)
                ImGui.ShowMetricsWindow(ref _metricsWindowVisible);

            SwapHoveringBuffers();
            TriggerGlobalActionsFromKeyBindings();
            DrawAppMenu();
        }


        private void TriggerGlobalActionsFromKeyBindings()
        {
            foreach (var (id, action) in UserActionRegistry.Entries)
            {
                if (KeyboardBinding.Triggered(id))
                {
                    action();
                }
            }
        }


        private void DrawAppMenu()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 6));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(6, 6));
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
                        UserActionRegistry.Entries[UserActions.Undo]();
                    }

                    if (ImGui.MenuItem("Redo", "CTRL+Y", false, UndoRedoStack.CanRedo))
                    {
                        UserActionRegistry.Entries[UserActions.Redo]();
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

                    if (ImGui.MenuItem("2nd Render Window", "", ShowSecondaryRenderWindow))
                        ShowSecondaryRenderWindow = !ShowSecondaryRenderWindow;

                    ImGui.Separator();

                    if (ImGui.MenuItem("ImGUI Demo", "", _demoWindowVisible))
                        _demoWindowVisible = !_demoWindowVisible;

                    if (ImGui.MenuItem("ImGUI Metrics", "", _metricsWindowVisible))
                        _metricsWindowVisible = !_metricsWindowVisible;

                    ImGui.EndMenu();
                }
                ImGui.EndMainMenuBar();
            }
            ImGui.PopStyleVar(2);
        }


        private IOutputUi GetSelectedOutputUi()
        {
            foreach (var gcw in GraphWindow.WindowInstances)
            {
                if (gcw.GraphCanvas.SelectionHandler.SelectedElements.Any())
                {
                    var outputUi = gcw.GraphCanvas.SelectionHandler.SelectedElements[0] as IOutputUi;
                    if (outputUi != null)
                        return outputUi;
                }
            }
            return null;
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
        
        public static bool ShowSecondaryRenderWindow { get; private set; }

        private List<ParameterWindow> _parameterWindows = new List<ParameterWindow>();

        public static readonly UiModel UiModel = new UiModel();

        private static T3UI _instance = null;

        private List<Window> _windows;
        private bool _demoWindowVisible = false;
        private bool _metricsWindowVisible = false;
    }
}
