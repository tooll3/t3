﻿#nullable enable
using System.IO;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SilkWindows;
using SilkWindows.Implementations.FileManager;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Core.Resource;
using T3.Core.SystemUi;
using T3.Core.Utils;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel.InputsAndTypes;
using T3.Editor.UiModel.ProjectHandling;
using T3.Serialization;

namespace T3.Editor.Gui.InputUi.SimpleInputUis;

public sealed class StringInputUi : InputValueUi<string>
{
    private const int MaxStringLength = 4000;

    public enum UsageType
    {
        Default,
        FilePath,
        DirectoryPath,
        Multiline,
        CustomDropdown,
    }

    public UsageType Usage { get; private set; } = UsageType.Default;
    public string? FileFilter { get; private set; }

    public override IInputUi Clone()
    {
        return new StringInputUi
                   {
                       InputDefinition = InputDefinition,
                       Parent = Parent,
                       PosOnCanvas = PosOnCanvas,
                       Relevancy = Relevancy,
                       Size = Size,
                       Usage = Usage,
                       FileFilter = FileFilter,
                   };
    }

    protected override InputEditStateFlags DrawEditControl(string name, Symbol.Child.Input input, ref string? value, bool readOnly)
    {
        value ??= string.Empty;

        var inputEditStateFlags = InputEditStateFlags.Nothing;

        switch (Usage)
        {
            case UsageType.Default:
                inputEditStateFlags = DrawDefaultTextEdit(ref value) ? InputEditStateFlags.Modified : InputEditStateFlags.Nothing;
                break;
            case UsageType.Multiline:
                inputEditStateFlags = DrawMultilineTextEdit(ref value);
                break;
            case UsageType.FilePath:
                inputEditStateFlags = DrawTypeAheadSearch(FileOperations.FilePickerTypes.File, ref value);
                ImGui.SameLine();
                if (ImGui.Button("Edit"))
                {
                    if (value != null)
                    {
                        ResourceManager.TryResolvePath(value, _searchResourceConsumer, out var absolutePath, out _);
                        if (!File.Exists(absolutePath))
                        {
                            Log.Error("Can't open non-existing file " + absolutePath);
                        }
                        else
                        {
                            CoreUi.Instance.OpenWithDefaultApplication(absolutePath);
                        }
                        //OpenFileManager(FileOperations.FilePickerTypes.File, _searchResourceConsumer.AvailableResourcePackages, new string[0], isFolder: false, async: true);
                    }
                }

                NormalizePathSeparators(inputEditStateFlags, ref value);
                break;
            case UsageType.DirectoryPath:
                inputEditStateFlags = DrawTypeAheadSearch(FileOperations.FilePickerTypes.Folder, ref value);
                
                NormalizePathSeparators(inputEditStateFlags, ref value);
                break;
            case UsageType.CustomDropdown:
                inputEditStateFlags = DrawCustomDropdown(input);
                break;
        }

        inputEditStateFlags |= ImGui.IsItemClicked() ? InputEditStateFlags.Started : InputEditStateFlags.Nothing;
        inputEditStateFlags |= ImGui.IsItemDeactivatedAfterEdit() ? InputEditStateFlags.Finished : InputEditStateFlags.Nothing;

        return inputEditStateFlags;

            
        static void NormalizePathSeparators(InputEditStateFlags inputEditStateFlags, ref string? value)
        {
            if (value == null)
                return;
                
            // normalize path separators when modified
            // use only forward slashes as windows is the only OS that supports backslashes
            if ((inputEditStateFlags & InputEditStateFlags.Modified) == InputEditStateFlags.Modified
                || (inputEditStateFlags & InputEditStateFlags.Finished) == InputEditStateFlags.Finished)
            {
                value = value.Replace('\\', '/');
                    
                // todo: handle trailing slashes
                //if (value.EndsWith('/'))
                //  value = value[..^1];
            }
        }
    }

    private InputEditStateFlags DrawTypeAheadSearch(FileOperations.FilePickerTypes type, ref string? value)
    {
        return DrawFileInput(type, ref value, FileFilter, Draw);
            
        static InputResult Draw(InputRequest request)
        {
            var fileExtensionFilters = request.FileExtensionFilters;
            var value = request.Value;
                
            var drawnItems = ResourceManager.EnumerateResources(fileExtensionFilters, request.IsFolder, request.ResourcePackageContainer.AvailableResourcePackages, ResourceManager.PathMode.Aliased);
                
            var args = new ResourceInputWithTypeAheadSearch.Args("##filePathSearch", drawnItems, request.ShowWarning);
            var changed = ResourceInputWithTypeAheadSearch.Draw(args, ref value, out _);
            return new InputResult(changed, value);
        }
    }
        
    private readonly record struct InputResult(bool Modified, string Value);

    private readonly record struct InputRequest(string Value, string[] FileExtensionFilters, bool IsFolder, bool ShowWarning, IResourceConsumer ResourcePackageContainer);

    private static InputEditStateFlags DrawFileInput(FileOperations.FilePickerTypes type, ref string? filePathValue, string? filter, Func<InputRequest, InputResult> draw)
    {
        ImGui.SetNextItemWidth(-70);

        var componentsNodeSelection = ProjectView.Focused?.NodeSelection;
        if (componentsNodeSelection == null)
            return InputEditStateFlags.Nothing;
        
        var selectedInstances = componentsNodeSelection.GetSelectedInstances().ToArray();
        var needsToGatherPackages = _searchResourceConsumer is null 
                                    || selectedInstances.Length != _selectedInstances.Length 
                                    || !selectedInstances.Except(_selectedInstances).Any();
        if (needsToGatherPackages)
        {
            var packagesInCommon = selectedInstances.PackagesInCommon().ToArray();
            _searchResourceConsumer = new TempResourceConsumer(packagesInCommon);
        }
            
        var isFolder = type == FileOperations.FilePickerTypes.Folder;
        var exists = ResourceManager.TryResolvePath(filePathValue, _searchResourceConsumer, out _, out _, isFolder);
            
        var warning = type switch
                          {
                              FileOperations.FilePickerTypes.File when !exists   => "File doesn't exist:\n",
                              FileOperations.FilePickerTypes.Folder when !exists => "Directory doesn't exist:\n",
                              _                                                  => string.Empty
                          };

        if (warning != string.Empty)
            ImGui.PushStyleColor(ImGuiCol.Text, UiColors.StatusAnimated.Rgba);

        var fileManagerOpen = _fileManagerOpen;
            
        if (fileManagerOpen)
        {
            ImGui.BeginDisabled();
        }
            
        string[] uiFilter;
        if(filter == null)
            uiFilter = Array.Empty<string>();
        else if (!filter.Contains('|'))
            uiFilter = [filter];
        else
            uiFilter = filter.Split('|')[1].Split(';');

        var fileFiltersInCommon = selectedInstances
                                 .Where(x => x is IDescriptiveFilename)
                                 .Cast<IDescriptiveFilename>()
                                 .Select(x => x.FileFilter)
                                 .Aggregate(Enumerable.Empty<string>(), (a, b) => a.Intersect(b))
                                 .Concat(uiFilter)
                                 .Where(s => !string.IsNullOrWhiteSpace(s))
                                 .Distinct()
                                 .ToArray();

        var inputEditStateFlags = InputEditStateFlags.Nothing;
        if(filePathValue != null && _searchResourceConsumer != null)
        {
            var result = draw(new InputRequest(filePathValue, fileFiltersInCommon, isFolder, ShowWarning: !exists, _searchResourceConsumer));
            filePathValue = result.Value;
            inputEditStateFlags = result.Modified ? InputEditStateFlags.Modified : InputEditStateFlags.Nothing;
        }

        if (warning != string.Empty)
            ImGui.PopStyleColor();

        if (ImGui.IsItemHovered() && filePathValue != null && filePathValue.Length > 0 && ImGui.CalcTextSize(filePathValue).X > ImGui.GetItemRectSize().X)
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(warning + filePathValue);
            ImGui.EndTooltip();
        }
            
        ImGui.SameLine();
        //var modifiedByPicker = FileOperations.DrawFileSelector(type, ref value, filter);
            
        if (ImGui.Button("...##fileSelector"))
        {
            if (_searchResourceConsumer != null)
            {
                OpenFileManager(type, _searchResourceConsumer.AvailableResourcePackages, fileFiltersInCommon, isFolder, async: true);
            }
            else
            {
                Log.Warning("Can open file manager with undefined resource consumer");
            }
        }
            
        if (fileManagerOpen)
        {
            ImGui.EndDisabled();
        }
            
        // refresh value because 
            
        string? fileManValue;
        lock (_fileManagerResultLock)
        {
            fileManValue = _latestFileManagerResult;
            _latestFileManagerResult = null;
        }
            
        var valueIsUpdated = !string.IsNullOrEmpty(fileManValue) && fileManValue != filePathValue;
            
        if (valueIsUpdated)
        {
            filePathValue = fileManValue;
            inputEditStateFlags |= InputEditStateFlags.Modified;
        }
            
        if (_hasClosedFileManager)
        {
            _hasClosedFileManager = false;
            inputEditStateFlags |= InputEditStateFlags.Finished;
        }
            
        return inputEditStateFlags;
    }
        
     
        
    private static bool DrawDefaultTextEdit(ref string value)
    {
        return ImGui.InputText("##textEdit", ref value, MaxStringLength);
    }

    private static InputEditStateFlags DrawMultilineTextEdit(ref string value)
    {
        ImGui.Dummy(new Vector2(1, 1));
        var changed = ImGui.InputTextMultiline("##textEdit", ref value, MaxStringLength, new Vector2(-1, 3 * ImGui.GetFrameHeight()));
        return changed ? InputEditStateFlags.Modified : InputEditStateFlags.Nothing;
    }

    private static InputEditStateFlags DrawCustomDropdown(Symbol.Child.Input input)
    {
        var instance = ProjectView.Focused?.NodeSelection.GetSelectedInstanceWithoutComposition();
        if (instance != null && instance is ICustomDropdownHolder customValueHolder)
        {
            var changed = false;

            var currentValue = customValueHolder.GetValueForInput(input.InputDefinition.Id);
                
            // A dropdown implementation that prevents free string input
            // if (ImGui.BeginCombo("##customDropdown", currentValue, ImGuiComboFlags.HeightLarge))
            // {
            //     foreach (var value2 in customValueHoder.GetOptionsForInput(input.InputDefinition.Id))
            //     {
            //         if (value2 == null)
            //             continue;
            //
            //         var isSelected = value2 == currentValue;
            //         if (!ImGui.Selectable($"{value2}", isSelected, ImGuiSelectableFlags.DontClosePopups))
            //             continue;
            //
            //         ImGui.CloseCurrentPopup();
            //         customValueHoder.HandleResultForInput(input.InputDefinition.Id, value2);
            //         changed = true;
            //     }
            //
            //     ImGui.EndCombo();
            // }

            var inputArgs = new InputWithTypeAheadSearch.Args<string>("##customDropdown", 
                                                                      customValueHolder.GetOptionsForInput(input.InputDefinition.Id), 
                                                                      GetTextInfo, 
                                                                      false);
            if (InputWithTypeAheadSearch.Draw(inputArgs, ref currentValue, out var selected))
            {
                ImGui.CloseCurrentPopup();
                customValueHolder.HandleResultForInput(input.InputDefinition.Id, selected);
                changed = true;
            }
                
            return changed ? InputEditStateFlags.Modified : InputEditStateFlags.Nothing;
        }
        else
        {
            ImGui.NewLine();
            //Log.Warning($"{instance?.Parent?.Symbol?.Name} doesn't support custom inputs");
            return InputEditStateFlags.Nothing;
        }
    }

    private static InputWithTypeAheadSearch.Texts GetTextInfo(string arg)
    {
        return new InputWithTypeAheadSearch.Texts(arg, arg, null);
    }

    protected override void DrawReadOnlyControl(string name, ref string? value)
    {
        if (value != null)
        {
            ImGui.InputText(name, ref value, MaxStringLength, ImGuiInputTextFlags.ReadOnly);
        }
        else
        {
            var nullString = "<null>";
            ImGui.InputText(name, ref nullString, MaxStringLength, ImGuiInputTextFlags.ReadOnly);
        }
    }

    public override bool DrawSettings()
    {
        var modified= base.DrawSettings();
        FormInputs.AddVerticalSpace();

        FormInputs.DrawFieldSetHeader("Usage");
        //var tmpForRef = selectedInputUi.Relevancy;
        //if (FormInputs.AddEnumDropdown(ref tmpForRef, null, defaultValue: Relevancy.Optional))
        //{
        {
            var tmpForRef = Usage;
            if (FormInputs.AddEnumDropdown(ref tmpForRef, null))
            {
                modified = true;
                Usage = tmpForRef;
            }
        }

        
        if (Usage == UsageType.FilePath)
        {
            FormInputs.DrawFieldSetHeader("File Filter");
            
            var tmp = FileFilter;
            var warning = !string.IsNullOrEmpty(tmp) && !tmp.Contains('|')
                              ? "Filter must include at least one | symbol.\nPlease read tooltip for examples"
                              : null;

            if (FormInputs.AddStringInput("##File Filter", ref tmp, null, warning,
                                          "This will only work for file FilePath-Mode.\nThe filter has to be in following format:\n\n Your Description (*.ext)|*.ext"))
            {
                modified = true;
                FileFilter = tmp;
            }
        }

        return modified;
    }

    public override void Write(JsonTextWriter writer)
    {
        base.Write(writer);

        writer.WriteObject(nameof(Usage), Usage.ToString());

        if (!string.IsNullOrEmpty(FileFilter))
            writer.WriteObject(nameof(FileFilter), FileFilter);
    }

    public override void Read(JToken? inputToken)
    {
        if (inputToken == null)
            return;

        base.Read(inputToken);

        var usageEnumToken = inputToken[nameof(Usage)];
        if (usageEnumToken != null && Enum.TryParse<UsageType>(usageEnumToken.Value<string>(), out var enumValue))
        {
            Usage = enumValue;
        }

        FileFilter = inputToken[nameof(FileFilter)]?.Value<string>();
    }
        
    private static void OpenFileManager(FileOperations.FilePickerTypes type, IEnumerable<IResourcePackage> packagesInCommon, string[] fileFiltersInCommon, bool isFolder, bool async)
    {
        var managedDirectories = packagesInCommon
                                .Concat(ResourceManager.GetSharedPackagesForFilters(fileFiltersInCommon, isFolder, out var culledFilters))
                                .Distinct()
                                .OrderBy(package => !package.IsReadOnly)
                                .Select(package => new ManagedDirectory(package.ResourcesFolder, package.IsReadOnly, !package.IsReadOnly, package.Alias));
            
        var fileManagerMode = type == FileOperations.FilePickerTypes.File ? FileManagerMode.PickFile : FileManagerMode.PickDirectory;
            
        Func<string, bool> filterFunc = culledFilters.Length == 0
                                            ? _ => true
                                            : str =>
                                              {
                                                  return culledFilters.Any(filter => StringUtils.MatchesSearchFilter(str, filter, ignoreCase: true));
                                              };
            
        var options = new SimpleWindowOptions(new Vector2(960, 600), 60, true, true, false);
        if (!async)
        {
            StartFileManagerBlocking();
        }
        else
        {
            StartFileManagerAsync();
        }
            
        return;
            
        void StartFileManagerAsync()
        {
            var fileManager = new FileManager(fileManagerMode, managedDirectories, filterFunc)
                                  {
                                      CloseOnResult = false,
                                      ClosingCallback = () =>
                                                        {
                                                            _hasClosedFileManager = true;
                                                            _fileManagerOpen = false;
                                                        }
                                  };
            _fileManagerOpen = true;
            _ = ImGuiWindowService.Instance.ShowAsync("Select a path", fileManager, (result) =>
                                                                                    {
                                                                                        lock(_fileManagerResultLock)
                                                                                            _latestFileManagerResult = result.RelativePathWithAlias ?? result.RelativePath;
                                                                                            
                                                                                    }, options);
        }
            
        void StartFileManagerBlocking()
        {
            var fileManager = new FileManager(fileManagerMode, managedDirectories, filterFunc);
            _fileManagerOpen = true;
            var fileManagerResult = ImGuiWindowService.Instance.Show("Select a path", fileManager, options);
            _fileManagerOpen = false;
                
            if (fileManagerResult != null)
            {
                lock (_fileManagerResultLock) // unnecessary, but consistent
                {
                    _latestFileManagerResult = fileManagerResult.RelativePathWithAlias ?? fileManagerResult.RelativePath;
                }
            }
                
            _hasClosedFileManager = true;
        }
    }
        
    private static string? _latestFileManagerResult;
    private static bool _fileManagerOpen;
    private static readonly object _fileManagerResultLock = new();
    private static bool _hasClosedFileManager;
    private static readonly Instance[] _selectedInstances = [];
    private static TempResourceConsumer? _searchResourceConsumer;
}