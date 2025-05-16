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
        var exists = ResourceManager.TryResolvePath(value, SearchResourceConsumer, out _, out _, isFolder);
            
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
        if(fileFilter == null)
            uiFilter = Array.Empty<string>();
        else if (!fileFilter.Contains('|'))
            uiFilter = [fileFilter];
        else
            uiFilter = fileFilter.Split('|')[1].Split(';');

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
        if(value != null && SearchResourceConsumer != null)
        {
            InputRequest request = new InputRequest(value, fileFiltersInCommon, isFolder, ShowWarning: !exists, SearchResourceConsumer);
            var fileExtensionFilters = request.FileExtensionFilters;
            var value1 = request.Value;
                
            var drawnItems = ResourceManager.EnumerateResources(fileExtensionFilters, request.IsFolder, request.ResourcePackageContainer.AvailableResourcePackages, ResourceManager.PathMode.Aliased);

            var changed = ResourceInputWithTypeAheadSearch.Draw("##filePathSearch", drawnItems, request.ShowWarning, ref value1, out _);
            
            var result = new InputResult(changed, value1);
            value = result.Value;
            inputEditStateFlags = result.Modified ? InputEditStateFlags.Modified : InputEditStateFlags.Nothing;
        }

        if (warning != string.Empty)
            ImGui.PopStyleColor();

        if (ImGui.IsItemHovered() && value != null && value.Length > 0 && ImGui.CalcTextSize(value).X > ImGui.GetItemRectSize().X)
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(warning + value);
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
            
        var valueIsUpdated = !string.IsNullOrEmpty(fileManValue) && fileManValue != value;
            
        if (valueIsUpdated)
        {
            value = fileManValue;
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