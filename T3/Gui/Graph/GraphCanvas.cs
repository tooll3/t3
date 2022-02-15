using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX.Direct3D11;
using T3.Compilation;
using T3.Core;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Commands;
using T3.Gui.Graph.Dialogs;
using T3.Gui.Graph.Interaction;
using T3.Gui.InputUi;
using T3.Gui.Interaction;
using T3.Gui.OutputUi;
using T3.Gui.Selection;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;
using T3.Gui.Windows;
using T3.Gui.Windows.Output;
using T3.Gui.Windows.TimeLine;
using UiHelpers;

namespace T3.Gui.Graph
{
    /// <summary>
    /// A <see cref="ICanvas"/> that displays the graph of an Operator.
    /// </summary>
    public class GraphCanvas : ScalableCanvas, INodeCanvas
    {
        public GraphCanvas(GraphWindow window, List<Guid> idPath)
        {
            //_selectionFence = new SelectionFence(this);
            _window = window;
            SetComposition(idPath, Transition.JumpIn);
        }

        public void SetComposition(List<Guid> childIdPath, Transition transition)
        {
            var previousFocusOnScreen = WindowPos + WindowSize / 2;

            var previousInstanceWasSet = _compositionPath != null && _compositionPath.Count > 0;
            if (previousInstanceWasSet)
            {
                var previousInstance = NodeOperations.GetInstanceFromIdPath(_compositionPath);
                UserSettings.Config.OperatorViewSettings[CompositionOp.SymbolChildId] = GetTargetScope();

                var newUiContainer = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
                var matchingChildUi = newUiContainer.ChildUis.FirstOrDefault(childUi => childUi.SymbolChild.Id == previousInstance.SymbolChildId);
                if (matchingChildUi != null)
                {
                    var centerOnCanvas = matchingChildUi.PosOnCanvas + matchingChildUi.Size / 2;
                    previousFocusOnScreen = TransformPosition(centerOnCanvas);
                }
            }

            _compositionPath = childIdPath;
            var comp = NodeOperations.GetInstanceFromIdPath(childIdPath);
            if (comp == null)
            {
                Log.Error("Can't resolve instance for id-path " + childIdPath);
                return;
            }

            CompositionOp = comp;

            SelectionManager.Clear();
            TimeLineCanvas.Current?.ClearSelection();

            var newProps = GuessViewProperties();
            if (CompositionOp != null)
            {
                UserSettings.SaveLastViewedOpForWindow(_window, CompositionOp.SymbolChildId);
                if (UserSettings.Config.OperatorViewSettings.ContainsKey(CompositionOp.SymbolChildId))
                    newProps = UserSettings.Config.OperatorViewSettings[CompositionOp.SymbolChildId];
            }

            SetScopeWithTransition(newProps.Scale, newProps.Scroll, previousFocusOnScreen, transition);
        }

        public void SetCompositionToChildInstance(Instance instance)
        {
            // Validation that instance is valid
            // TODO: only do in debug mode
            var op = NodeOperations.GetInstanceFromIdPath(_compositionPath);
            var matchingChild = op.Children.SingleOrDefault(child => child == instance);
            if (matchingChild == null)
            {
                throw new ArgumentException("Can't OpenChildNode because Instance is not a child of current composition");
            }

            var newPath = _compositionPath;
            newPath.Add(instance.SymbolChildId);
            SelectionManager.Clear();
            TimeLineCanvas.Current?.ClearSelection();
            SetComposition(newPath, Transition.JumpIn);
        }

        public void SetCompositionToParentInstance(Instance instance)
        {
            if (instance == null)
            {
                Log.Warning("can't jump to parent with invalid instance");
                return;
            }

            var previousCompositionOp = CompositionOp;
            var shortenedPath = new List<Guid>();
            foreach (var pathItemId in _compositionPath)
            {
                if (pathItemId == instance.SymbolChildId)
                    break;

                shortenedPath.Add(pathItemId);
            }

            shortenedPath.Add(instance.SymbolChildId);

            if (shortenedPath.Count() == _compositionPath.Count())
                throw new ArgumentException("Can't SetCompositionToParentInstance because Instance is not a parent of current composition");

            SetComposition(shortenedPath, Transition.JumpOut);
            SelectionManager.Clear();
            TimeLineCanvas.Current?.ClearSelection();
            var previousCompChildUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id].ChildUis
                                                      .SingleOrDefault(childUi => childUi.Id == previousCompositionOp.SymbolChildId);
            if (previousCompChildUi != null)
                SelectionManager.AddSymbolChildToSelection(previousCompChildUi, previousCompositionOp);
        }

        private Scope GuessViewProperties()
        {
            ChildUis = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id].ChildUis;
            FocusViewToSelection();
            return GetTargetScope();
        }

        public void MakeCurrent()
        {
            Current = this;
        }

        #region drawing UI ====================================================================
        public void Draw(ImDrawListPtr dl, bool showGrid)
        {
            // TODO: Refresh reference on every frame. Since this uses lists instead of dictionary
            // it can be really slow
            CompositionOp = NodeOperations.GetInstanceFromIdPath(_compositionPath);
            if (CompositionOp == null)
            {
                Log.Error("unable to get composition op");
                return;
            }

            UpdateCanvas();
            if (this.CompositionOp == null)
            {
                Log.Error("Can't show graph for undefined CompositionOp");
                return;
            }

            GraphBookmarkNavigation.HandleForCanvas(this);

            MakeCurrent();
            ChildUis = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id].ChildUis;
            DrawList = dl;
            ImGui.BeginGroup();
            {
                DrawDropHandler();

                if (KeyboardBinding.Triggered(UserActions.FocusSelection))
                    FocusViewToSelection();

                if (KeyboardBinding.Triggered(UserActions.Duplicate))
                {
                    var selectedChildren = GetSelectedChildUis();
                    CopySelectionToClipboard(selectedChildren);
                    PasteClipboard();
                }

                if (KeyboardBinding.Triggered(UserActions.DeleteSelection))
                    DeleteSelectedElements();

                if (KeyboardBinding.Triggered(UserActions.ToggleDisabled))
                    ToggleDisabledForSelectedElements();

                if (KeyboardBinding.Triggered(UserActions.PinToOutputWindow))
                    PinSelectedToOutputWindow();


                
                if (KeyboardBinding.Triggered(UserActions.CopyToClipboard))
                {
                    var selectedChildren = GetSelectedChildUis();
                    if (selectedChildren.Any())
                        CopySelectionToClipboard(selectedChildren);
                }

                if (KeyboardBinding.Triggered(UserActions.PasteFromClipboard))
                {
                    PasteClipboard();
                }

                if (KeyboardBinding.Triggered(UserActions.LayoutSelection))
                {
                    SelectableNodeMovement.ArrangeOps();
                }

                DrawList.PushClipRect(WindowPos, WindowPos + WindowSize);

                if (showGrid)
                    DrawGrid();

                if (ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem))
                {
                    ConnectionMaker.ConnectionSplitHelper.PrepareNewFrame(this);
                }

                _symbolBrowser.Draw();

                Graph.DrawGraph(DrawList);
                RenameInstanceOverlay.Draw();
                HandleFenceSelection();

                var isOnBackground = ImGui.IsWindowFocused() && !ImGui.IsAnyItemActive();
                if (isOnBackground && ImGui.IsMouseDoubleClicked(0))
                {
                    SetCompositionToParentInstance(CompositionOp.Parent);
                }

                if (ConnectionMaker.TempConnections.Count > 0 && ImGui.IsMouseReleased(0))
                {
                    var isAnyItemHovered = ImGui.IsAnyItemHovered();
                    var droppedOnBackground =
                        ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem | ImGuiHoveredFlags.AllowWhenBlockedByPopup) && !isAnyItemHovered;
                    if (droppedOnBackground)
                    {
                        ConnectionMaker.InitSymbolBrowserAtPosition(_symbolBrowser,
                                                                    InverseTransformPosition(ImGui.GetIO().MousePos));
                    }
                    else
                    {
                        if (ConnectionMaker.TempConnections[0].GetStatus() != ConnectionMaker.TempConnection.Status.TargetIsDraftNode)
                        {
                            ConnectionMaker.Cancel();
                        }
                    }
                }

                DrawList.PopClipRect();
                DrawContextMenu();

                _duplicateSymbolDialog.Draw(CompositionOp, GetSelectedChildUis(), ref _nameSpaceForDialogEdits, ref _symbolNameForDialogEdits,
                                            ref _symbolDescriptionForDialog);
                _combineToSymbolDialog.Draw(CompositionOp, GetSelectedChildUis(), ref _nameSpaceForDialogEdits, ref _symbolNameForDialogEdits,
                                            ref _symbolDescriptionForDialog);
                _renameSymbolDialog.Draw(GetSelectedChildUis(), ref _symbolNameForDialogEdits);
                _addInputDialog.Draw(CompositionOp.Symbol);
                _addOutputDialog.Draw(CompositionOp.Symbol);
                EditNodeOutputDialog.Draw();
            }
            ImGui.EndGroup();
            Current = null;
        }

        private void HandleFenceSelection()
        {
            _fenceState = SelectionFence.UpdateAndDraw(_fenceState);
            switch (_fenceState)
            {
                case SelectionFence.States.PressedButNotMoved:
                    if (SelectionFence.SelectMode == SelectionFence.SelectModes.Replace)
                        SelectionManager.Clear();
                    break;

                case SelectionFence.States.Updated:
                    HandleSelectionFenceUpdate(SelectionFence.BoundsInScreen);
                    break;

                case SelectionFence.States.CompletedAsClick:
                    SelectionManager.Clear();
                    SelectionManager.SetSelectionToParent(CompositionOp);
                    break;
            }
        }

        private SelectionFence.States _fenceState = SelectionFence.States.Inactive;

        private void HandleSelectionFenceUpdate(ImRect boundsInScreen)
        {
            var boundsInCanvas = InverseTransformRect(boundsInScreen);
            var nodesToSelect = (from child in SelectableChildren
                                 let rect = new ImRect(child.PosOnCanvas, child.PosOnCanvas + child.Size)
                                 where rect.Overlaps(boundsInCanvas)
                                 select child).ToList();

            SelectionManager.Clear();
            foreach (var node in nodesToSelect)
            {
                if (node is SymbolChildUi symbolChildUi)
                {
                    var instance = CompositionOp.Children.FirstOrDefault(child => child.SymbolChildId == symbolChildUi.Id);
                    if (instance == null)
                    {
                        Log.Warning("Can't find instance");
                    }

                    SelectionManager.AddSymbolChildToSelection(symbolChildUi, instance);
                }
                else
                {
                    SelectionManager.AddSelection(node);
                }
            }
        }

        /// <remarks>
        /// This method is completed, because it has to handle several edge cases and has potential to remove previous user data:
        /// - We have to preserve the previous state.
        /// - We have to make space -> Shift all connected operators towards the right.
        /// - We have to convert all existing connections from the output into temporary connections.
        /// - We have to insert a new temp connection line between output and symbol browser
        ///
        /// - If the user completes the symbol browser, it must complete the previous connections from the temp connections.
        /// - If the user cancels the operation, the previous state has to be restored. This might be tricky
        /// </remarks>
        public void OpenSymbolBrowserForOutput(SymbolChildUi childUi, Symbol.OutputDefinition outputDef)
        {
            ConnectionMaker.InitSymbolBrowserAtPosition(_symbolBrowser,
                                                        childUi.PosOnCanvas + new Vector2(childUi.Size.X + SelectableNodeMovement.SnapPadding.X, 0));
        }

        private Symbol GetSelectedSymbol()
        {
            var selectedChildUi = GetSelectedChildUis().FirstOrDefault();
            return selectedChildUi != null ? selectedChildUi.SymbolChild.Symbol : CompositionOp.Symbol;
        }

        private void DrawDropHandler()
        {
            if (!T3Ui.DraggingIsInProgress)
                return;

            ImGui.SetCursorPos(Vector2.Zero);
            ImGui.InvisibleButton("## drop", ImGui.GetWindowSize());

            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload("Symbol");
                if (ImGui.IsMouseReleased(0))
                {
                    var myString = Marshal.PtrToStringAuto(payload.Data);
                    if (myString != null)
                    {
                        var guidString = myString.Split('|')[0];
                        var guid = Guid.Parse(guidString);
                        Log.Debug("dropped symbol here" + payload + " " + myString + "  " + guid);

                        var symbol = SymbolRegistry.Entries[guid];
                        var parent = CompositionOp.Symbol;
                        var posOnCanvas = InverseTransformPosition(ImGui.GetMousePos());
                        var childUi = NodeOperations.CreateInstance(symbol, parent, posOnCanvas);

                        var instance = CompositionOp.Children.Single(child => child.SymbolChildId == childUi.Id);
                        SelectionManager.SetSelectionToChildUi(childUi, instance);

                        T3Ui.DraggingIsInProgress = false;
                    }
                }

                ImGui.EndDragDropTarget();
            }
        }

        public IEnumerable<Symbol> GetParentSymbols()
        {
            return NodeOperations.GetParentInstances(CompositionOp, includeChildInstance: true).Select(p => p.Symbol);
        }

        private void FocusViewToSelection()
        {
            FitAreaOnCanvas(GetSelectionBounds());
        }

        private ImRect GetSelectionBounds(float padding = 50)
        {
            var selectedOrAll = SelectionManager.IsAnythingSelected()
                                    ? SelectionManager.GetSelectedNodes<ISelectableNode>().ToArray()
                                    : SelectableChildren.ToArray();

            if (selectedOrAll.Length == 0)
                return new ImRect();

            var firstElement = selectedOrAll[0];
            var bounds = new ImRect(firstElement.PosOnCanvas, firstElement.PosOnCanvas + Vector2.One);
            foreach (var element in selectedOrAll)
            {
                bounds.Add(element.PosOnCanvas);
                bounds.Add(element.PosOnCanvas + element.Size);
            }

            bounds.Expand(padding);
            return bounds;
        }

        private void DrawContextMenu()
        {
            if (T3Ui.OpenedPopUpName == string.Empty)
            {
                CustomComponents.DrawContextMenuForScrollCanvas(DrawContextMenuContent, ref _contextMenuIsOpen);
            }
        }

        private void DrawContextMenuContent()
        {
            var selectedChildUis = GetSelectedChildUis();
            var nextUndoTitle = UndoRedoStack.CanUndo ? $" ({UndoRedoStack.GetNextUndoTitle()})" : string.Empty;
            if (ImGui.MenuItem("Undo" + nextUndoTitle,
                               shortcut: KeyboardBinding.ListKeyboardShortcuts(UserActions.Undo, false),
                               selected: false,
                               enabled: UndoRedoStack.CanUndo))
            {
                UndoRedoStack.Undo();
            }

            ImGui.Separator();

            // ------ for selection -----------------------
            var oneOpSelected = selectedChildUis.Count == 1;
            var someOpsSelected = selectedChildUis.Count > 0;

            var label = oneOpSelected
                            ? $"{selectedChildUis[0].SymbolChild.ReadableName}..."
                            : $"{selectedChildUis.Count} selected items...";

            ImGui.PushFont(Fonts.FontSmall);
            ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
            ImGui.TextUnformatted(label);
            ImGui.PopStyleColor();
            ImGui.PopFont();

            // Enable / Disable
            var allSelectedDisabled = selectedChildUis.TrueForAll(selectedChildUi => selectedChildUi.IsDisabled);
            var allSelectedEnabled = selectedChildUis.TrueForAll(selectedChildUi => !selectedChildUi.IsDisabled);
            if (!allSelectedDisabled && ImGui.MenuItem("Disable",
                                                       KeyboardBinding.ListKeyboardShortcuts(UserActions.ToggleDisabled, false),
                                                       selected:false,
                                                       enabled: selectedChildUis.Count >0 ))
            {
                var commands = new List<ICommand>();
                foreach (var selectedChildUi in selectedChildUis)
                {
                    commands.Add(new ChangeInstanceIsDisabledCommand(selectedChildUi, true));
                }

                UndoRedoStack.AddAndExecute(new MacroCommand("Disable operators", commands));
            }

            if (!allSelectedEnabled && ImGui.MenuItem("Enable", 
                                                      KeyboardBinding.ListKeyboardShortcuts(UserActions.ToggleDisabled, false),
                                                      selected: false,
                                                      enabled: someOpsSelected))
            {
                var commands = new List<ICommand>();
                foreach (var selectedChildUi in selectedChildUis)
                {
                    commands.Add(new ChangeInstanceIsDisabledCommand(selectedChildUi, false));
                }

                UndoRedoStack.AddAndExecute(new MacroCommand("Enable operators", commands));
            }

            if (ImGui.MenuItem("Rename", oneOpSelected))
            {
                RenameInstanceOverlay.OpenForSymbolChildUi(selectedChildUis[0]);
            }
            
            if (ImGui.MenuItem("Arrange sub graph",
                               KeyboardBinding.ListKeyboardShortcuts(UserActions.LayoutSelection, false),
                               selected: false,
                               enabled: someOpsSelected))
            {
                SelectableNodeMovement.ArrangeOps();
            }
            
            if (ImGui.BeginMenu("Display as..."))
            {
                if (ImGui.MenuItem("Small", "", 
                                   selected: selectedChildUis.Any(child => child.Style == SymbolChildUi.Styles.Default), 
                                   enabled: someOpsSelected))
                {
                    foreach (var childUi in selectedChildUis)
                    {
                        childUi.Style = SymbolChildUi.Styles.Default;
                    }
                }

                if (ImGui.MenuItem("Resizable", "", 
                                   selected: selectedChildUis.Any(child => child.Style == SymbolChildUi.Styles.Resizable),
                                   enabled:someOpsSelected))
                {
                    foreach (var childUi in selectedChildUis)
                    {
                        childUi.Style = SymbolChildUi.Styles.Resizable;
                    }
                }

                if (ImGui.MenuItem("Expanded", "", 
                                   selected:selectedChildUis.Any(child => child.Style == SymbolChildUi.Styles.Resizable),
                                   enabled:someOpsSelected))
                {
                    foreach (var childUi in selectedChildUis)
                    {
                        childUi.Style = SymbolChildUi.Styles.Expanded;
                    }
                }
                
                ImGui.Separator();

                var isImage =oneOpSelected 
                             && selectedChildUis[0].SymbolChild.Symbol.OutputDefinitions.Count > 0
                             && selectedChildUis[0].SymbolChild.Symbol.OutputDefinitions[0].ValueType == typeof(Texture2D);
                if (ImGui.MenuItem("Set image as graph background",
                                   "",
                                   selected: false,
                                   enabled: isImage))
                {
                    var instance =CompositionOp.Children.Single(child => child.SymbolChildId == selectedChildUis[0].Id);
                    GraphWindow.SetBackgroundOutput(instance);
                }

                if (ImGui.MenuItem("Pin to output", oneOpSelected))
                {
                    PinSelectedToOutputWindow();
                } 


                ImGui.EndMenu();
            }

            if (ImGui.MenuItem("Export", oneOpSelected))
            {
                ExportInstance(selectedChildUis.Single());
            }
            
            ImGui.Separator();

            if (ImGui.MenuItem("Copy",
                               KeyboardBinding.ListKeyboardShortcuts(UserActions.CopyToClipboard, false),
                               selected:false,
                               enabled:someOpsSelected))
            {
                CopySelectionToClipboard(selectedChildUis);
            }

            if (ImGui.MenuItem("Paste", KeyboardBinding.ListKeyboardShortcuts(UserActions.PasteFromClipboard, false)))
            {
                PasteClipboard();
            }

            var selectedInputUis = GetSelectedInputUis().ToArray();
            var selectedOutputUis = GetSelectedOutputUis().ToArray();
            
            if (ImGui.MenuItem("Delete", 
                               shortcut:"Del",  // dynamic assigned shortcut is too long
                               selected:false,
                               enabled:someOpsSelected || selectedInputUis.Length > 0 || selectedOutputUis.Length > 0))
            {
                DeleteSelectedElements();

                if (selectedInputUis.Length > 0)
                {
                    var symbol = GetSelectedSymbol();
                    NodeOperations.RemoveInputsFromSymbol(selectedInputUis.Select(entry => entry.Id).ToArray(), symbol);
                }

                
                if (selectedOutputUis.Length > 0)
                {
                    var symbol = GetSelectedSymbol();
                    NodeOperations.RemoveOutputsFromSymbol(selectedOutputUis.Select(entry => entry.Id).ToArray(), symbol);
                }
            }

            if (ImGui.MenuItem("Duplicate",
                               KeyboardBinding.ListKeyboardShortcuts(UserActions.Duplicate, false),
                               selected: false,
                               enabled: selectedChildUis.Count > 0))
            {
                CopySelectionToClipboard(selectedChildUis);
                PasteClipboard();
            }

            ImGui.Separator();
            if (ImGui.BeginMenu("Symbol definition..."))
            {
                if (ImGui.MenuItem("Rename Symbol", oneOpSelected))
                {
                    _renameSymbolDialog.ShowNextFrame();
                    _symbolNameForDialogEdits = selectedChildUis[0].SymbolChild.Symbol.Name;
                    //NodeOperations.RenameSymbol(selectedChildUis[0].SymbolChild.Symbol, "NewName");
                }

                if (ImGui.MenuItem("Duplicate as new type...", oneOpSelected))
                {
                    _symbolNameForDialogEdits = selectedChildUis[0].SymbolChild.Symbol.Name ?? string.Empty;
                    _nameSpaceForDialogEdits = selectedChildUis[0].SymbolChild.Symbol.Namespace ?? string.Empty;
                    _symbolDescriptionForDialog = "";
                    _duplicateSymbolDialog.ShowNextFrame();
                }

                if (ImGui.MenuItem("Combine into new type...", someOpsSelected))
                {
                    _nameSpaceForDialogEdits = CompositionOp.Symbol.Namespace ?? string.Empty;
                    _symbolDescriptionForDialog = "";
                    _combineToSymbolDialog.ShowNextFrame();
                }

                ImGui.EndMenu();
            }
            //}

            if (ImGui.BeginMenu("Add..."))
            {
                if (ImGui.MenuItem("Add Node..."))
                {
                    _symbolBrowser.OpenAt(InverseTransformPosition(ImGui.GetMousePos()), null, null, false, null);
                }

                if (ImGui.MenuItem("Add input parameter..."))
                {
                    _addInputDialog.ShowNextFrame();
                }

                if (ImGui.MenuItem("Add output..."))
                {
                    _addOutputDialog.ShowNextFrame();
                }

                ImGui.EndMenu();
            }
        }

        private void PinSelectedToOutputWindow()
        {
            var outputWindow = OutputWindow.OutputWindowInstances.FirstOrDefault(ow => ow.Config.Visible) as OutputWindow;
            if (outputWindow == null)
            {
                Log.Warning("Can't pin selection without visible output window");
                return;
            }

            var selection = GetSelectedChildUis();
            if (selection.Count != 1)
            {
                Log.Warning("Please select only one operator to pin to output window");
                return;                
            }
            
            var instance = CompositionOp.Children.Single(child => child.SymbolChildId == selection[0].Id);
            if (instance != null)
            {
                outputWindow.Pinning.PinInstance(instance);
            }
        }

        public class ExportInfo 
        {
            public HashSet<Instance> CollectedInstances { get; } = new HashSet<Instance>();
            public HashSet<Symbol> UniqueSymbols { get; } = new HashSet<Symbol>();
            public HashSet<string> UniqueResourcePaths { get; } = new HashSet<string>();

            public bool AddInstance(Instance instance)
            {
                if (CollectedInstances.Contains(instance))
                    return false;

                CollectedInstances.Add(instance);
                return true;
            }

            public void AddResourcePath(string path)
            {
                UniqueResourcePaths.Add(path);
            }

            public bool AddSymbol(Symbol symbol) 
            {
                if (UniqueSymbols.Contains(symbol))
                    return false;

                UniqueSymbols.Add(symbol);
                return true;
            }

            public void PrintInfo()
            {
                Log.Info($"Collected {CollectedInstances.Count} instances for export in {UniqueSymbols.Count} different symbols");
                foreach (var resourcePath in UniqueResourcePaths)
                {
                    Log.Info(resourcePath);
                }
            }
        }

        private void ExportInstance(SymbolChildUi childUi)
        {
            Log.Info("export");
            // collect all ops and types
            var instance = CompositionOp.Children.Single(child => child.SymbolChildId == childUi.Id);
            if (instance.Outputs.Count >= 1 && instance.Outputs.First().ValueType == typeof(Texture2D))
            {
                // Update project settings
                ProjectSettings.Config.MainOperatorName = instance.Symbol.Name;
                ProjectSettings.Save();
                
                // traverse starting at output and collect everything
                var exportInfo = new ExportInfo();
                CollectChildSymbols(instance.Symbol, exportInfo);
                
                string exportDir = "Export";
                try
                {
                    Directory.Delete(exportDir, true);
                }
                catch (Exception)
                {
                    // ignored
                }
                Directory.CreateDirectory(exportDir);

                // generate Operators assembly
                var operatorAssemblySources = exportInfo.UniqueSymbols.Select(symbol =>
                                                                    {
                                                                        var source = File.ReadAllText(symbol.SourcePath);
                                                                        return source;
                                                                    }).ToList();
                operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\GpuQuery.cs"));
                operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\BmFont.cs"));
                var references = OperatorUpdating.CompileSymbolsFromSource(exportDir, operatorAssemblySources.ToArray());
                
                // copy player and dependent assemblies to export dir
                var currentDir = Directory.GetCurrentDirectory();
                var playerFileNames = new List<string>
                                          {
                                              currentDir + @"\Player\bin\Release\bass.dll",
                                              currentDir + @"\Player\bin\Release\basswasapi.dll",
                                              currentDir + @"\Player\bin\Release\CommandLine.dll",
                                              currentDir + @"\Player\bin\Release\Player.exe",
                                              currentDir + @"\Player\bin\Release\Player.exe.config",
                                              currentDir + @"\Player\bin\Release\SharpDX.Desktop.dll",
                                              currentDir + @"\Player\bin\Release\System.Runtime.CompilerServices.Unsafe.dll",  // required for svg
                                          };
                playerFileNames.ForEach(s => CopyFile(s, exportDir));
                
                var referencedAssemblies = references.Where(r => r.Display.Contains(currentDir))
                                                     .Select(r => r.Display)
                                                     .Distinct()
                                                     .ToArray();
                foreach (var asmPath in referencedAssemblies)
                {
                    CopyFile(asmPath, exportDir);
                }

                // generate exported .t3 files
                Json json = new Json();
                string symbolExportDir = exportDir + Path.DirectorySeparatorChar + @"Operators\Types\";
                if (Directory.Exists(symbolExportDir))
                    Directory.Delete(symbolExportDir, true);
                Directory.CreateDirectory(symbolExportDir);
                foreach (var symbol in exportInfo.UniqueSymbols)
                {
                    using (var sw = new StreamWriter(symbolExportDir + symbol.Name + "_" + symbol.Id + ".t3"))
                    using (var writer = new JsonTextWriter(sw))
                    {
                        json.Writer = writer;
                        json.Writer.Formatting = Formatting.Indented;
                        json.WriteSymbol(symbol);
                    }
                }
                
                // copy referenced resources
                Traverse(instance.Outputs.First(), exportInfo);
                exportInfo.PrintInfo();
                var resourcePaths = exportInfo.UniqueResourcePaths;
                resourcePaths.Add(ProjectSettings.Config.SoundtrackFilepath);
                resourcePaths.Add(@"projectSettings.json");
                resourcePaths.Add(@"Resources\hash-functions.hlsl");
                resourcePaths.Add(@"Resources\noise-functions.hlsl");
                resourcePaths.Add(@"Resources\particle.hlsl");
                resourcePaths.Add(@"Resources\pbr.hlsl");
                resourcePaths.Add(@"Resources\point.hlsl");
                resourcePaths.Add(@"Resources\point-light.hlsl");
                resourcePaths.Add(@"Resources\utils.hlsl");
                resourcePaths.Add(@"Resources\lib\dx11\fullscreen-texture.hlsl");
                resourcePaths.Add(@"Resources\lib\img\internal\resolve-multisampled-depth-buffer-cs.hlsl");
                resourcePaths.Add(@"Resources\lib\particles\particle-dead-list-init.hlsl");
                resourcePaths.Add(@"Resources\t3\t3.ico");
                foreach (var resourcePath in resourcePaths)
                {
                    try
                    {
                        var targetPath = exportDir + Path.DirectorySeparatorChar + resourcePath;
                    
                        var targetDir = new DirectoryInfo(targetPath).Parent.FullName;
                        if (!Directory.Exists(targetDir))
                            Directory.CreateDirectory(targetDir);
                        
                        File.Copy(resourcePath, targetPath);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Error exporting resource '{resourcePath}': '{e.Message}'");
                    }
                }
            } 
            else
            {
                Log.Warning("Can only export ops with 'Texture2D' output");
            }
        }

        public static void CopyFile(string sourcePath, string targetDir)
        {
            var fi = new FileInfo(sourcePath);
            var targetPath = targetDir + Path.DirectorySeparatorChar + fi.Name;
            try
            {
                File.Copy(sourcePath, targetPath);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to copy resource file for export: {sourcePath}  {e.Message}");
            }
        }

        public static void CollectChildSymbols(Symbol symbol, ExportInfo exportInfo)
        {
            if (!exportInfo.AddSymbol(symbol))
                return; // already visited

            foreach (var symbolChild in symbol.Children)
            {
                CollectChildSymbols(symbolChild.Symbol, exportInfo);
            }
        }

        public static void Traverse(ISlot slot, ExportInfo exportInfo)
        {
            if (slot is IInputSlot)
            {
                if (slot.IsConnected)
                {
                    Traverse(slot.GetConnection(0), exportInfo);
                }

                CheckInputForResourcePath(slot, exportInfo);
            }
            else if (slot.IsConnected)
            {
                // slot is an output of an composition op
                Traverse(slot.GetConnection(0), exportInfo);
                exportInfo.AddInstance(slot.Parent);
            }
            else
            {
                Instance parent = slot.Parent;
                // Log.Info(parent.Symbol.Name);
                if (!exportInfo.AddInstance(parent))
                    return; // already visited

                foreach (var input in parent.Inputs)
                {
                    CheckInputForResourcePath(input, exportInfo);

                    if (input.IsConnected)
                    {
                        if (input.IsMultiInput)
                        {
                            var multiInput = (IMultiInputSlot)input;
                            foreach (var entry in multiInput.GetCollectedInputs())
                            {
                                Traverse(entry, exportInfo);
                            }
                        }
                        else
                        {
                            Traverse(input.GetConnection(0), exportInfo);
                        }
                    }
                }
            }
        }

        private static void CheckInputForResourcePath(ISlot inputSlot, ExportInfo exportInfo)
        {
            var parent = inputSlot.Parent;
            var inputUi = SymbolUiRegistry.Entries[parent.Symbol.Id].InputUis[inputSlot.Id];
            if (inputUi is StringInputUi stringInputUi && stringInputUi.Usage == StringInputUi.UsageType.FilePath)
            {
                var compositionSymbol = parent.Parent.Symbol;
                var parentSymbolChild = compositionSymbol.Children.Single(child => child.Id == parent.SymbolChildId);
                var value = parentSymbolChild.InputValues[inputSlot.Id].Value;
                if (value is InputValue<string> stringValue)
                {
                    var resourcePath = stringValue.Value;
                    exportInfo.AddResourcePath(resourcePath);
                    if (resourcePath.EndsWith(".fnt"))
                    {
                        exportInfo.AddResourcePath(resourcePath.Replace(".fnt", ".png"));
                    }
                }
            }
        }

        private bool _contextMenuIsOpen;

        private void DeleteSelectedElements()
        {
            var selectedChildren = GetSelectedChildUis();
            if (selectedChildren.Any())
            {
                var compositionSymbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
                var cmd = new DeleteSymbolChildCommand(compositionSymbolUi, selectedChildren);
                UndoRedoStack.AddAndExecute(cmd);
            }

            var selectedInputUis = SelectionManager.GetSelectedNodes<IInputUi>().ToList();
            if (selectedInputUis.Count > 0)
            {
                NodeOperations.RemoveInputsFromSymbol(selectedInputUis.Select(entry => entry.Id).ToArray(), CompositionOp.Symbol);
            }

            SelectionManager.Clear();
        }

        private void ToggleDisabledForSelectedElements()
        {
            var selectedChildren = GetSelectedChildUis();

            
            var isNodeHovered = T3Ui.HoveredIdsLastFrame.Count == 1 && CompositionOp != null;
            if (isNodeHovered)
            {
                var hoveredChildUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id].ChildUis
                                                     .SingleOrDefault(c => c.Id == T3Ui.HoveredIdsLastFrame.First());
                if (hoveredChildUi == null)
                    return;
                
                selectedChildren = new List<SymbolChildUi> { hoveredChildUi };
            }
            
            // if (!selectedChildren.Any())
            // {
            // }

            var allSelectedDisabled = selectedChildren.TrueForAll(selectedChildUi => selectedChildUi.IsDisabled);
            var shouldDisable = !allSelectedDisabled;

            var commands = new List<ICommand>();
            foreach (var selectedChildUi in selectedChildren)
            {
                commands.Add(new ChangeInstanceIsDisabledCommand(selectedChildUi, shouldDisable));
            }

            UndoRedoStack.AddAndExecute(new MacroCommand("Disable/Enable", commands));
        }

        private static List<SymbolChildUi> GetSelectedChildUis()
        {
            return SelectionManager.GetSelectedNodes<SymbolChildUi>().ToList();
        }

        private IEnumerable<IInputUi> GetSelectedInputUis()
        {
            return SelectionManager.GetSelectedNodes<IInputUi>();
        }

        private IEnumerable<IOutputUi> GetSelectedOutputUis()
        {
            return SelectionManager.GetSelectedNodes<IOutputUi>();
        }

        private void CopySelectionToClipboard(List<SymbolChildUi> selectedChildren)
        {
            var containerOp = new Symbol(typeof(object), Guid.NewGuid());
            var newContainerUi = new SymbolUi(containerOp);
            SymbolUiRegistry.Entries.Add(newContainerUi.Symbol.Id, newContainerUi);

            var compositionSymbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
            var cmd = new CopySymbolChildrenCommand(compositionSymbolUi, selectedChildren, newContainerUi,
                                                    InverseTransformPosition(ImGui.GetMousePos()));
            cmd.Do();

            using (var writer = new StringWriter())
            {
                var json = new Json { Writer = new JsonTextWriter(writer) { Formatting = Formatting.Indented } };
                json.Writer.WriteStartArray();

                json.WriteSymbol(containerOp);

                var jsonUi = new UiJson { Writer = json.Writer };
                jsonUi.WriteSymbolUi(newContainerUi);

                json.Writer.WriteEndArray();

                try
                {
                    Clipboard.SetText(writer.ToString(), TextDataFormat.UnicodeText);
                    //Log.Info(Clipboard.GetText(TextDataFormat.UnicodeText));
                }
                catch (Exception)
                {
                    Log.Error("Could not copy elements to clipboard. Perhaps a tool like TeamViewer locks it.");
                }
            }

            SymbolUiRegistry.Entries.Remove(newContainerUi.Symbol.Id);
        }

        private void PasteClipboard()
        {
            try
            {
                var text = Clipboard.GetText();
                using (var reader = new StringReader(text))
                {
                    var json = new Json { Reader = new JsonTextReader(reader) };
                    if (!(JToken.ReadFrom(json.Reader) is JArray o))
                        return;

                    var symbolJson = o[0];
                    var containerSymbol = json.ReadSymbol(null, symbolJson, true);
                    SymbolRegistry.Entries.Add(containerSymbol.Id, containerSymbol);

                    var symbolUiJson = o[1];
                    var containerSymbolUi = UiJson.ReadSymbolUi(symbolUiJson);
                    var compositionSymbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
                    SymbolUiRegistry.Entries.Add(containerSymbolUi.Symbol.Id, containerSymbolUi);
                    var cmd = new CopySymbolChildrenCommand(containerSymbolUi, null, compositionSymbolUi,
                                                            InverseTransformPosition(ImGui.GetMousePos()));
                    cmd.Do(); // FIXME: Shouldn't this be UndoRedoQueue.AddAndExecute() ? 
                    SymbolUiRegistry.Entries.Remove(containerSymbolUi.Symbol.Id);
                    SymbolRegistry.Entries.Remove(containerSymbol.Id);

                    // Select new operators
                    SelectionManager.Clear();

                    foreach (var id in cmd.NewSymbolChildIds)
                    {
                        var newChildUi = compositionSymbolUi.ChildUis.Single(c => c.Id == id);
                        var instance = CompositionOp.Children.Single(c2 => c2.SymbolChildId == id);
                        SelectionManager.AddSymbolChildToSelection(newChildUi, instance);
                    }
                }
            }
            catch (Exception)
            {
                Log.Warning("Could not copy actual selection to clipboard.");
            }
        }

        private void DrawGrid()
        {
            var color = new Color(0, 0, 0, 0.3f);
            var gridSize = Math.Abs(64.0f * Scale.X);
            for (var x = (-Scroll.X*Scale.X) % gridSize; x < WindowSize.X; x += gridSize)
            {
                DrawList.AddLine(new Vector2(x, 0.0f) + WindowPos,
                                 new Vector2(x, WindowSize.Y) + WindowPos,
                                 color);
            }

            for (var y = (-Scroll.Y*Scale.Y) % gridSize; y < WindowSize.Y; y += gridSize)
            {
                DrawList.AddLine(
                                 new Vector2(0.0f, y) + WindowPos,
                                 new Vector2(WindowSize.X, y) + WindowPos,
                                 color);
            }
        }

        public IEnumerable<ISelectableNode> SelectableChildren
        {
            get
            {
                _selectableItems.Clear();
                _selectableItems.AddRange(ChildUis);
                var symbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
                _selectableItems.AddRange(symbolUi.InputUis.Values);
                _selectableItems.AddRange(symbolUi.OutputUis.Values);

                return _selectableItems;
            }
        }

        private readonly List<ISelectableNode> _selectableItems = new List<ISelectableNode>();
        #endregion

        #region public API
        /// <summary>
        /// The canvas that is currently being drawn from the UI.
        /// Note that <see cref="GraphCanvas"/> is NOT a singleton so you can't rely on this to be valid outside of the Drawing() context.
        /// </summary>
        public static GraphCanvas Current { get; private set; }

        public ImDrawListPtr DrawList { get; private set; }
        public Instance CompositionOp { get; private set; }
        #endregion

        private List<Guid> _compositionPath = new List<Guid>();

        private readonly AddInputDialog _addInputDialog = new AddInputDialog();
        private readonly AddOutputDialog _addOutputDialog = new AddOutputDialog();
        private readonly CombineToSymbolDialog _combineToSymbolDialog = new CombineToSymbolDialog();
        private readonly DuplicateSymbolDialog _duplicateSymbolDialog = new DuplicateSymbolDialog();
        private readonly RenameSymbolDialog _renameSymbolDialog = new RenameSymbolDialog();
        public readonly EditNodeOutputDialog EditNodeOutputDialog = new EditNodeOutputDialog();

        //public override SelectionHandler SelectionHandler { get; } = new SelectionHandler();
        private List<SymbolChildUi> ChildUis { get; set; }
        public readonly SymbolBrowser _symbolBrowser = new SymbolBrowser();
        private string _symbolNameForDialogEdits = "";
        private string _symbolDescriptionForDialog = "";
        private string _nameSpaceForDialogEdits = "";
        private readonly GraphWindow _window;

        public enum HoverModes
        {
            Disabled,
            Live,
            LastValue,
        }
    }
}