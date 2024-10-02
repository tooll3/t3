using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.InputUi.SimpleInputUis;
using T3.Editor.Gui.Styling;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Windows.Utilities;

public static class FileAssetsHelper
{
    private static FileResource _activeFileResource;
    private static bool _wasFilenameChanged;

    public static void Draw()
    {
        if (ImGui.Button("Rescan Assets"))
        {
            ScanResources();
        }

        CustomComponents.DrawInputFieldWithPlaceholder("Filter", ref _filter);

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2, 2));
        foreach (var (path, r) in _resources.OrderBy(r => r.Key))
        {
            if (!string.IsNullOrEmpty(_filter) && !path.Contains(_filter))
                continue;

            ImGui.PushID(path);
            ImGui.AlignTextToFramePadding();

            ImGui.TextUnformatted($"{r.InputUsages.Count + r.DefaultValueUsages.Count}");
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ListUsages(r.DefaultValueUsages, "Default Values...");
                ListUsages(r.InputUsages, "Inputs...");
                ImGui.EndTooltip();
            }

            CustomComponents.TooltipForLastItem(" ");

            ImGui.SameLine(50);
            ImGui.PushStyleColor(ImGuiCol.Text, r.State == FileResource.States.Missing ? UiColors.StatusWarning : UiColors.Text.Rgba);
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.4f * ImGui.GetStyle().Alpha);
            ImGui.TextUnformatted(r.Directory + "\\");
            ImGui.PopStyleVar();
            ImGui.SameLine();

            if (_activeFileResource == r)
            {
                ImGui.SetKeyboardFocusHere();
                if (CustomComponents.DrawInputFieldWithPlaceholder("Filename", ref r.FileName, 400 * T3Ui.UiScaleFactor, false))
                {
                    _wasFilenameChanged = true;
                }

                if (ImGui.IsItemDeactivated())
                {
                    // Apply changes...
                    if( _wasFilenameChanged)
                    {
                        var newPath = Path.Combine(r.Directory, r.FileName);
                        foreach (var usage in r.InputUsages)
                        {
                            usage.InputValue.Value = newPath;
                            usage.SymbolUi.FlagAsModified();
                        }

                        foreach (var usage in r.DefaultValueUsages)
                        {
                            usage.InputValue.Value = newPath;
                            usage.SymbolUi.FlagAsModified();
                        }

                        try
                        {
                            Log.Debug($"Renaming {path} -> {r.FileName}");
                            File.Move(path, newPath);
                        }
                        catch (Exception e)
                        {
                            Log.Warning("Failed: " + e.Message);
                        }
                        
                        T3Ui.SaveModified();
                        ScanResources();
                        _wasFilenameChanged = false;
                        _activeFileResource = null;
                        break;
                    }
                    _activeFileResource = null;
                }
            }
            else
            {
                ImGui.TextUnformatted(r.FileName);
                if (ImGui.IsItemClicked())
                {
                    _activeFileResource = r;
                }
            }

            ImGui.PopStyleColor();
            ImGui.PopID();
        }

        ImGui.PopStyleVar();
    }

    private static void ListUsages(List<InputUsage> rDefaultValueUsages, string label)
    {
        if (rDefaultValueUsages.Count <= 0)
            return;
        
        CustomComponents.HelpText(label);
        foreach (var inputUsage in rDefaultValueUsages)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
            ImGui.TextUnformatted($"{inputUsage.SymbolUi.Symbol.Name} / ");
            ImGui.PopStyleColor();

            ImGui.SameLine();
            ImGui.TextUnformatted($"{inputUsage.StringInputUi.Parent.Symbol.Name}");

            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
            ImGui.TextUnformatted($".{inputUsage.StringInputUi.InputDefinition.Name}");
            ImGui.PopStyleColor();
        }
    }

    private static string _filter = "";

    private static void ScanResources()
    {
        _resources.Clear();
        foreach (var symbol in SymbolRegistry.Entries.Values)
        {
            if (!SymbolUiRegistry.Entries.TryGetValue(symbol.Id, out var symbolUi))
                continue;

            // Default values
            foreach (var inputDef in symbol.InputDefinitions)
            {
                if (!symbolUi.InputUis.TryGetValue(inputDef.Id, out var inputUi))
                    continue;

                if (inputUi is not StringInputUi stringInputUi)
                    continue;
                
                if (stringInputUi.Usage != StringInputUi.UsageType.FilePath)
                    continue;
                
                if (inputDef.DefaultValue is not InputValue<string> stringValue)
                    continue;
                
                var path = stringValue.Value;
                
                AddResourceUsage(path, out var resource);
                resource.DefaultValueUsages.Add(new InputUsage
                                             {
                                                 InputValue = stringValue,
                                                 StringInputUi = stringInputUi,
                                                 SymbolUi = symbolUi,
                                             });
            }
            
            // Collect children
            foreach (var symbolChild in symbol.Children)
            {
                var childUi = symbolUi.ChildUis.FirstOrDefault(sui => sui.Id == symbolChild.Id);
                if (childUi == null)
                    continue;

                if (!SymbolUiRegistry.Entries.TryGetValue(childUi.SymbolChild.Symbol.Id, out var childUiSymbolUi))
                    continue;

                foreach (var (inputId, input) in symbolChild.Inputs)
                {
                    if (input.Value is not InputValue<string> stringValue)
                        continue;

                    if (!childUiSymbolUi.InputUis.TryGetValue(inputId, out var inputUi))
                        continue;

                    if (inputUi is not StringInputUi stringInputUi)
                        continue;

                    if (stringInputUi.Usage != StringInputUi.UsageType.FilePath)
                        continue;

                    var path = stringValue.Value;
                    
                    AddResourceUsage(path, out var resource);
                    resource.InputUsages.Add(new InputUsage
                                                        {
                                                            InputValue = stringValue,
                                                            StringInputUi = stringInputUi,
                                                            SymbolUi = symbolUi,
                                                        });
                }
            }
        }
    }

    private static void AddResourceUsage(string path, out FileResource resource)
    {
        if (_resources.TryGetValue(path, out resource))
            return;
        
        resource = new FileResource
                       {
                           Directory = Path.GetDirectoryName(path),
                           FileName = Path.GetFileName(path),
                           InputUsages = new(),
                           DefaultValueUsages = new(),
                           State = FileResource.States.Unused
                       };

        resource.State = !File.Exists(path) ? FileResource.States.Missing : FileResource.States.Used;

        _resources.Add(path, resource);
    }

    private static readonly Dictionary<string, FileResource> _resources = new();

    class FileResource
    {
        public string Directory;
        public string FileName;
        public List<InputUsage> InputUsages;
        public List<InputUsage> DefaultValueUsages;
        public States State;

        public enum States
        {
            Unused,
            Missing,
            Used,
        }
    }

    private struct InputUsage
    {
        public InputValue<string> InputValue;
        public StringInputUi StringInputUi;
        public SymbolUi SymbolUi;
    }
}