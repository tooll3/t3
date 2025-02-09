using System.Diagnostics;
using ImGuiNET;
using T3.Editor.Gui.MagGraph.Model;
using T3.Editor.Gui.MagGraph.States;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.MagGraph.Interaction;

internal class OutputPicking
{
    internal static void Init(GraphUiContext context)
    {
        WindowContentExtend.GetLastAndReset();
    }

    internal static void Reset(GraphUiContext context)
    {
        //context.TempConnections.Clear();
        //context.ActiveSourceItem = null;
        //context.DraggedPrimaryOutputType = null;
    }

    internal static void DrawHiddenOutputSelector(GraphUiContext context)
    {
        Debug.Assert(context.StateMachine.CurrentState == GraphStates.PickOutput);

        if (context.ActiveItem == null)
        {
            return;
        }

        var screenPos = context.Canvas.TransformPosition(context.ActiveItem.PosOnCanvas + context.ActiveItem.Size);

        ImGui.SetNextWindowPos(screenPos);

        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 5);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.One * 4);
        ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

        ImGui.PushStyleColor(ImGuiCol.ChildBg, UiColors.BackgroundFull.Fade(0.7f).Rgba);
        ImGui.PushStyleColor(ImGuiCol.Button, UiColors.BackgroundFull.Fade(0.0f).Rgba);

        var lastSize = WindowContentExtend.GetLastAndReset();
        if(ImGui.BeginPopup("pickOutput", ImGuiWindowFlags.Popup))
        // if (ImGui.BeginChild("Popup",
        //                      lastSize,
        //                      true,
        //                      ImGuiWindowFlags.NoResize
        //                      | ImGuiWindowFlags.NoScrollbar
        //                      | ImGuiWindowFlags.AlwaysUseWindowPadding
        //                     ))
        {
            CustomComponents.HintLabel("Additional outputs...");
            WindowContentExtend.ExtendToLastItem();

            foreach (var o in context.ActiveItem.SymbolUi!.OutputUis.Values)
            {
                var isVisible = false;
                foreach (var visibleOutput in context.ActiveItem.OutputLines)
                {
                    if (visibleOutput.Id != o.Id) continue;
                    isVisible = true;
                    break;
                }

                if (isVisible)
                    continue;

                var name = o.OutputDefinition.Name;
                var labelSize = ImGui.CalcTextSize(name);
                var width = MathF.Max(lastSize.X - 4, labelSize.X + 30);
                var buttonSize = new Vector2(width, ImGui.GetFrameHeight());

                ImGui.Button(name, buttonSize);
                
                if(ImGui.IsItemHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    var outputSlot = context.ActiveItem?.Instance?.Outputs.FirstOrDefault(os => os.Id == o.Id);
                    if (outputSlot == null)
                        break;

                    // if (!context.TryGetActiveOutputLine(out var outputLine))
                    // {
                    //     Log.Warning("Output not found?");
                    //     context.StateMachine.SetState(Default, context);
                    //     return;
                    // }

                    //var outputLine = context.GetActiveOutputLine();              
                    // var output = outputLine.Output;
                    // var posOnCanvas = sourceItem.PosOnCanvas + new Vector2(MagGraphItem.GridSize.X,
                    //                                                        MagGraphItem.GridSize.Y * (1.5f + outputLine.VisibleIndex));

                    var tempConnection = new MagGraphConnection
                                             {
                                                 Style = MagGraphConnection.ConnectionStyles.Unknown,
                                                 SourcePos = context.ActiveItem.PosOnCanvas,
                                                 TargetPos = default,
                                                 SourceItem = context.ActiveItem,
                                                 TargetItem = null,
                                                 SourceOutput = outputSlot,
                                                 OutputLineIndex = 0,
                                                 VisibleOutputIndex = 0,
                                                 ConnectionHash = 0,
                                                 IsTemporary = true,
                                             };
                    context.TempConnections.Add(tempConnection);
                    context.ActiveSourceItem = context.ActiveItem;
                    context.ActiveSourceOutputId = outputSlot.Id;
                    context.DraggedPrimaryOutputType = outputSlot.ValueType;
                    context.StateMachine.SetState(GraphStates.DragConnectionEnd, context);
                    //Reset(context);
                }

                WindowContentExtend.ExtendToLastItem();

                // if (!string.IsNullOrEmpty(inputUi.Description))
                // {
                //     CustomComponents.TooltipForLastItem(inputUi.Description);
                // }

                // ImGui.TextUnformatted(o.OutputDefinition.Name);
                // ImGui.SameLine();
                // ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
                // ImGui.TextUnformatted($" <{o.OutputDefinition.ValueType.Name}>");
                // WindowContentExtend.ExtendToLastItem();
                // ImGui.PopStyleColor();
            }
            ImGui.EndPopup();
        }
        else
        {
            // Reset on release?
            context.StateMachine.SetState(GraphStates.Default, context);
            //Log.Debug("here");
        }
        //ImGui.EndChild();

        ImGui.PopStyleVar(5);
        ImGui.PopStyleColor(2);
    }
}