#nullable enable
using System.Threading;
using ImGuiNET;
using SilkWindows;
using SilkWindows.Implementations.FileManager;
using T3.Core.Operator.Interfaces;
using T3.Core.Resource;
using T3.Core.Utils;
using T3.Editor.Gui.Styling;
using T3.Editor.UiModel.InputsAndTypes;
using T3.Editor.UiModel.ProjectHandling;

namespace T3.Editor.Gui.UiHelpers;

/// <summary>
/// Handles drawing a project file resource picker e.g. for StringInputUis our Soundtracks.
/// </summary>
internal static class FilePickingUi
{
    public static InputEditStateFlags DrawTypeAheadSearch(FileOperations.FilePickerTypes type, string? fileFilter, ref string? value)
    {
        return DrawFileInput(type, ref value, fileFilter, Draw);
            
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

    private static InputEditStateFlags DrawFileInput(FileOperations.FilePickerTypes type, ref string? filePathValue, string? filter, Func<InputRequest, InputResult> draw)
    {
        ImGui.SetNextItemWidth(-70);

        var nodeSelection = ProjectView.Focused?.NodeSelection;
        if (ProjectView.Focused?.CompositionInstance == null || nodeSelection == null)
            return InputEditStateFlags.Nothing;
        
        var selectedInstances = nodeSelection.GetSelectedInstances().ToArray();
        var needsToGatherPackages = true; //SearchResourceConsumer is null;

        if (selectedInstances.Length == 0)
        {
            SearchResourceConsumer = new TempResourceConsumer(ProjectView.Focused.CompositionInstance.AvailableResourcePackages);
        }
        else
        {
            // Check later...            
            // || selectedInstances.Length != StringInputUi._selectedInstances.Length 
            // || !selectedInstances.Except(StringInputUi._selectedInstances).Any();
            if (needsToGatherPackages)
            {
                var packagesInCommon = selectedInstances.PackagesInCommon().ToArray();
                SearchResourceConsumer = new TempResourceConsumer(packagesInCommon);
            }
        }
            
            
        var isFolder = type == FileOperations.FilePickerTypes.Folder;
        var exists = ResourceManager.TryResolvePath(filePathValue, SearchResourceConsumer, out _, out _, isFolder);
            
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
        if(filePathValue != null && SearchResourceConsumer != null)
        {
            var result = draw(new InputRequest(filePathValue, fileFiltersInCommon, isFolder, ShowWarning: !exists, SearchResourceConsumer));
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
            if (SearchResourceConsumer != null)
            {
                OpenFileManager(type, SearchResourceConsumer.AvailableResourcePackages, fileFiltersInCommon, isFolder, async: true);
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
    private static readonly Lock _fileManagerResultLock = new();
    private static bool _hasClosedFileManager;
    public static TempResourceConsumer? SearchResourceConsumer;

    private readonly record struct InputResult(bool Modified, string Value);

    private readonly record struct InputRequest(string Value, string[] FileExtensionFilters, bool IsFolder, bool ShowWarning, IResourceConsumer ResourcePackageContainer);
}