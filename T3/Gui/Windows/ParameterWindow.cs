using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Commands;
using t3.Gui.Commands.Graph;
using T3.Gui.Graph;
using T3.Gui.Graph.Dialogs;
using T3.Gui.Graph.Interaction;
using T3.Gui.InputUi;
using T3.Gui.Styling;

namespace T3.Gui.Windows
{
    class ParameterWindow : Window
    {
        public ParameterWindow()
        {
            _instanceCounter++;
            Config.Title = "Parameters##" + _instanceCounter;
            AllowMultipleInstances = true;
            Config.Visible = true;

            _parameterWindowInstances.Add(this);
        }

        public override List<Window> GetInstances()
        {
            return _parameterWindowInstances;
        }

        protected override void UpdateBeforeDraw()
        {
        }

        protected override void DrawAllInstances()
        {
            // Convert to array to allow closing of windowns
            foreach (var w in _parameterWindowInstances.ToArray())
            {
                w.DrawOneInstance();
            }
        }

        protected override void Close()
        {
            _parameterWindowInstances.Remove(this);
        }

        protected override void AddAnotherInstance()
        {
            // ReSharper disable once ObjectCreationAsStatement
            new ParameterWindow(); // Required to call constructor
        }

        protected override void DrawContent()
        {
            // Insert invisible spill over input to catch accidental imgui focus attempts
            {
                ImGui.SetNextItemWidth(2);
                string tmpBuffer = "";
                ImGui.InputText("##imgui workaround", ref tmpBuffer, 1);
                ImGui.SameLine();
            }
            
            var instance = NodeSelection.GetFirstSelectedInstance();
            if (instance != null)
            {
                if (instance.Parent == null)
                    return;

                _editDescriptionDialog.Draw(instance.Symbol);
                
                var parentUi = SymbolUiRegistry.Entries[instance.Parent.Symbol.Id];
                var symbolChildUi = parentUi.ChildUis.Single(childUi => childUi.Id == instance.SymbolChildId);
                var symbolUi = SymbolUiRegistry.Entries[instance.Symbol.Id];

                if (DrawSelectedSymbolHeader(instance, symbolChildUi))
                {
                    symbolUi.FlagAsModified();
                }

                var compositionSymbolUi = SymbolUiRegistry.Entries[instance.Parent.Symbol.Id];
                var selectedChildSymbolUi = SymbolUiRegistry.Entries[instance.Symbol.Id];

                // Draw parameters
                DrawParameters(instance, selectedChildSymbolUi, symbolChildUi, compositionSymbolUi);

                ImGui.PushFont(Fonts.FontSmall);

                ImGui.Dummy(new Vector2(10,10));
                ImGui.Indent();
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(10,10));


                if (!string.IsNullOrEmpty(symbolUi.Description))
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
                    ImGui.TextWrapped(symbolUi.Description);
                    ImGui.PopStyleColor();                        
                }
                
                if (ImGui.Button("Edit description..."))
                    _editDescriptionDialog.ShowNextFrame();                
                
                SymbolBrowser.ListExampleOperators(symbolUi);
                if (!string.IsNullOrEmpty(symbolUi.Description))
                {
                    var itemRegex = new Regex(@"\[([A-Za-z\d_]+)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    var alreadyListedSymbolNames = new HashSet<string>();
                    
                    foreach (Match  match in itemRegex.Matches(symbolUi.Description))
                    {
                        var referencedName = match.Groups[1].Value;

                        if (referencedName == symbolUi.Symbol.Name)
                            continue;

                        if (alreadyListedSymbolNames.Contains(referencedName))
                            continue;
                        
                        // This is slow and could be optimized by dictionary
                        var referencedSymbolUi = SymbolRegistry.Entries.Values.SingleOrDefault(s => s.Name == referencedName);
                        if (referencedSymbolUi != null)
                        {
                            SymbolBrowser.DrawExampleOperator(referencedSymbolUi.Id,referencedName);
                        }

                        alreadyListedSymbolNames.Add(referencedName);
                    }
                }
                
                ImGui.PopStyleVar();
                ImGui.Unindent();
                
                ImGui.PopFont();
                return;
            }

            if (!NodeSelection.IsAnythingSelected())
                return;

            // Draw parameter Settings
            foreach (var input in NodeSelection.GetSelectedNodes<IInputUi>())
            {
                ImGui.PushID(input.Id.GetHashCode());
                ImGui.PushFont(Fonts.FontLarge);
                ImGui.TextUnformatted("Parameter settings for " + input.InputDefinition.Name);
                ImGui.PopFont();
                input.DrawSettings();
                ImGui.Spacing();
                ImGui.PopID();
            }
            
            // ImGui.Separator();
            // Draw Annotation settings
            foreach (var annotation in NodeSelection.GetSelectedNodes<Annotation>())
            {
                ImGui.PushID(annotation.Id.GetHashCode());
                ImGui.PushFont(Fonts.FontLarge);
                ImGui.TextUnformatted("Annotation settings" + annotation.Title);
                ImGui.PopFont();

                ImGui.ColorEdit4("color", ref annotation.Color.Rgba);
                ImGui.PopID();
            }
            
        }

        
        
        public static void DrawParameters(Instance instance, SymbolUi symbolUi, SymbolChildUi symbolChildUi,
                                          SymbolUi compositionSymbolUi)
        {
            foreach (var inputSlot in instance.Inputs)
            {
                if (!symbolUi.InputUis.TryGetValue(inputSlot.Id, out IInputUi inputUi))
                {
                    Log.Warning("Trying to access an non existing input, probably the op instance is not the actual one.");
                    continue;
                }

                ImGui.PushID(inputSlot.Id.GetHashCode());
                var editState = inputUi.DrawInputEdit(inputSlot, compositionSymbolUi, symbolChildUi);

                if ((editState & InputEditStateFlags.Started) != InputEditStateFlags.Nothing)
                {
                    _inputSlotForActiveCommand = inputSlot;
                    _inputValueCommandInFlight = new ChangeInputValueCommand(instance.Parent.Symbol, instance.SymbolChildId, inputSlot.Input, inputSlot.Input.Value);
                }
                
                if ((editState & InputEditStateFlags.Modified) != InputEditStateFlags.Nothing)
                {
                    if (_inputValueCommandInFlight == null || _inputSlotForActiveCommand != inputSlot)
                    {
                         _inputValueCommandInFlight = new ChangeInputValueCommand(instance.Parent.Symbol, instance.SymbolChildId, inputSlot.Input, inputSlot.Input.Value);
                         _inputSlotForActiveCommand = inputSlot;
                    }
                    _inputValueCommandInFlight.AssignNewValue(inputSlot.Input.Value);
                    inputSlot.DirtyFlag.Invalidate();
                }
                
                if ((editState & InputEditStateFlags.Finished) != InputEditStateFlags.Nothing)
                {
                    if (_inputValueCommandInFlight != null && _inputSlotForActiveCommand == inputSlot)
                    {
                        UndoRedoStack.Add(_inputValueCommandInFlight);
                    }
                    else
                    {
                        Log.Debug($"finished but wrong inputSlot {inputSlot.Input.Name}");
                    }
                    _inputValueCommandInFlight = null;
                }

                if (editState == InputEditStateFlags.ShowOptions)
                {
                    NodeSelection.SetSelection(inputUi);
                }
                
                ImGui.PopID();
            }
        }

        private bool DrawSelectedSymbolHeader(Instance op, SymbolChildUi symbolChildUi)
        {
            var modified = false;
            
            // namespace and symbol
            {
                ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.One * 5);
                
                ImGui.PushFont(Fonts.FontBold);

                ImGui.TextUnformatted(op.Symbol.Name);
                ImGui.PopFont();
                
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, new Color(0.5f).Rgba);
                ImGui.TextUnformatted(" in ");
                ImGui.PopStyleColor();
                ImGui.SameLine();
                
                ImGui.PushStyleColor(ImGuiCol.Text, new Color(0.5f).Rgba);
                var namespaceForEdit = op.Symbol.Namespace ?? "";

                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X-10);
                if (InputWithTypeAheadSearch.Draw("##namespace", ref namespaceForEdit,
                                                  SymbolRegistry.Entries.Values.Select(i => i.Namespace).Distinct().OrderBy(i => i)))
                {
                    op.Symbol.Namespace = namespaceForEdit;
                    modified = true;
                }
                
                ImGui.PopStyleColor();
            }

            // SymbolChild Name
            {
                ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.One * 5);
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 105);

                var nameForEdit = symbolChildUi.SymbolChild.Name;

                if (ImGui.InputText("##symbolChildName", ref nameForEdit, 128))
                {
                    _symbolChildNameCommand.NewName = nameForEdit;
                    symbolChildUi.SymbolChild.Name = nameForEdit;
                }


                if (ImGui.IsItemActivated())
                {
                    _symbolChildNameCommand = new ChangeSymbolChildNameCommand(symbolChildUi, op.Parent.Symbol);
                }

                if (ImGui.IsItemDeactivatedAfterEdit())
                {
                    UndoRedoStack.Add(_symbolChildNameCommand);
                    _symbolChildNameCommand = null;
                }

                // Fake placeholder text
                if (string.IsNullOrEmpty(nameForEdit))
                    ImGui.GetWindowDrawList().AddText(ImGui.GetItemRectMin() + new Vector2(5, 5),
                                                      T3Style.Colors.TextMuted,
                                                      "Untitled instance");

                ImGui.SameLine();
            }

            // Disabled toggle
            {
                ImGui.PushFont(Fonts.FontBold);
                if (symbolChildUi.IsDisabled)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, T3Style.Colors.WarningColor.Rgba);
                    ImGui.PushStyleColor(ImGuiCol.Text, Color.White.Rgba);
                    if (ImGui.Button("DISABLED", new Vector2(100, 0)))
                    {
                        UndoRedoStack.AddAndExecute(new ChangeInstanceIsDisabledCommand(symbolChildUi, false));
                    }
                    ImGui.PopStyleColor(2);
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, T3Style.Colors.TextMuted.Rgba);
                    if (ImGui.Button("ENABLED", new Vector2(100, 0)))
                    {
                        UndoRedoStack.AddAndExecute(new ChangeInstanceIsDisabledCommand(symbolChildUi, true));
                    }

                    ImGui.PopStyleColor();
                }

                ImGui.PopFont();
            }

            ImGui.Dummy(new Vector2(0.0f, 5.0f));
            return modified;
        }

        public static bool IsAnyInstanceVisible()
        {
            return T3Ui.WindowManager.IsAnyInstanceVisible<ParameterWindow>();
        }
        
        private static readonly EditSymbolDescriptionDialog _editDescriptionDialog = new EditSymbolDescriptionDialog();
        private static readonly List<Window> _parameterWindowInstances = new List<Window>();
        private ChangeSymbolChildNameCommand _symbolChildNameCommand;
        private static ChangeInputValueCommand _inputValueCommandInFlight;
        private static IInputSlot _inputSlotForActiveCommand;
        private static int _instanceCounter;
    }
}