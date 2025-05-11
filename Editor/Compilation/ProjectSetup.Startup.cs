#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using T3.Core.Compilation;
using T3.Core.Model;
using T3.Core.Resource;
using T3.Editor.External;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using T3.Editor.UiModel.Exporting;

namespace T3.Editor.Compilation;

internal static partial class ProjectSetup
{
    internal static bool TryLoadAll(bool forceRecompile, [NotNullWhen(false)] out Exception? exception)
    {
        try
        {
            LoadAll(forceRecompile);

            exception = null;
            return true;
        }
        catch (Exception e)
        {
            exception = e;
            return false;
        }
    }

    /// <summary>
    /// Loads all projects and packages - for use only at the start of the editor
    /// </summary>
    /// <param name="forceRecompile">all symbol projects will be recompiled if possible</param>
    /// <exception cref="Exception">Unknown exceptions may be raised - if you want to handle them, wrap this in a try/catch</exception>
    private static void LoadAll(bool forceRecompile)
    {
        // ReSharper disable once RedundantAssignment
        bool isDebugBuild = false;
            
        #if DEBUG
        isDebugBuild = true;
        Stopwatch totalStopwatch = new();
        totalStopwatch.Start();
        #endif

        if (isDebugBuild)
        {
            // Load pre-built built-in packages as read-only
            LoadBuiltInPackages();
        }

        // Find project files
        var csProjFiles = FindCsProjFiles(isDebugBuild);

        // Load projects
        LoadProjects(csProjFiles, forceRecompile, failedProjects: out _);

        // Register UI types
        var allPackages = ActivePackages.Values
                                        .ToArray();
        
        UiRegistration.RegisterUiTypes();
        
        foreach (var package in allPackages)
        {
            ((EditorSymbolPackage)package.Package).InitializeCustomUis();
        }

        // Update all symbol packages
        UpdateSymbolPackages(allPackages);
        
        // Initialize resources and shader linting
        foreach (var package in allPackages)
        {
            InitializePackageResources(package);
        }
        

        ShaderLinter.AddPackage(SharedResources.ResourcePackage, ResourceManager.SharedShaderPackages);

        // Initialize custom UIs

        foreach (var package in SymbolPackage.AllPackages)
        {
            Log.Debug($"Completed loading {package.DisplayName}");
        }

        #if DEBUG
        totalStopwatch.Stop();
        Log.Info($"Total load time: {totalStopwatch.ElapsedMilliseconds}ms");
        #endif
    }

    private static void LoadBuiltInPackages()
    {
        var directory = Directory.CreateDirectory(CoreOperatorDirectory);

        directory
           .EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
           .Where(folder => !folder.Name.EndsWith(PlayerExporter.ExportFolderName, StringComparison.OrdinalIgnoreCase)) // ignore "player" project directory
           .ToList()
           .ForEach(directoryInfo =>
                    {
                        if (!AssemblyInformation.TryCreateFromReleasedPackage(directoryInfo.FullName, out var assembly, out var releaseInfo))
                        {
                            Log.Error($"Failed to load assembly from directory \"{directoryInfo.FullName}\"");
                            return;
                        }

                        AddToLoadedPackages(new PackageWithReleaseInfo(new EditorSymbolPackage(assembly), releaseInfo));
                    });
    }

    [SuppressMessage("ReSharper", "OutParameterValueIsAlwaysDiscarded.Local")]
    private static void LoadProjects(FileInfo[] csProjFiles, bool forceRecompile, out List<ProjectWithReleaseInfo> failedProjects)
    {
        // Load each project file and its associated assembly
        var releases = csProjFiles
                      .AsParallel()
                      .Select(fileInfo =>
                              {
                                  if (!CsProjectFile.TryLoad(fileInfo.FullName, out var loadInfo))
                                  {
                                      Log.Error($"Failed to load project at \"{fileInfo.FullName}\":\n{loadInfo.Error}");
                                      return new ProjectWithReleaseInfo(fileInfo, null, null);
                                  }
                                  
                                  var csProjFile = loadInfo.CsProjectFile!;

                                  bool success;
                                  ReleaseInfo? info = null;

                                  if (forceRecompile)
                                  {
                                      success = csProjFile.TryRecompile(out info, true);
                                  }
                                  else
                                  {
                                      success = csProjFile.TryGetReleaseInfo(out info) ||
                                                csProjFile.TryRecompile(out info, true);
                                  }

                                  // this may call for some reworking of how that works (MSBuild actions in C#?), or generation at project creation time
                                  if (!success)
                                  {
                                      Log.Error($"Failed to load release info for {csProjFile.Name}");
                                      return new ProjectWithReleaseInfo(fileInfo, csProjFile, null);
                                  }

                                  return new ProjectWithReleaseInfo(fileInfo, csProjFile, info);
                              })
                      .ToArray();

        failedProjects = [];
        foreach (var release in releases)
        {
            if (release.ReleaseInfo != null)
            {
                var package = new PackageWithReleaseInfo(new EditableSymbolProject(release.CsProject!), release.ReleaseInfo);
                AddToLoadedPackages(package);
            }
            else
            {
                failedProjects.Add(release);
            }
        }
    }

    private static FileInfo[] FindCsProjFiles(bool includeBuiltInAsProjects)
    {
        return GetProjectDirectories(includeBuiltInAsProjects)
              .SelectMany(dir => Directory.EnumerateFiles(dir, "*.csproj", SearchOption.AllDirectories))
              .Select(x => new FileInfo(x))
              .ToArray();
        

        static IEnumerable<string> GetProjectDirectories(bool includeBuiltInAsProjects)
        {
            // ReSharper disable once JoinDeclarationAndInitializer
            string[] topDirectories = [UserSettings.Config.DefaultNewProjectDirectory];

            var projectSearchDirectories = topDirectories
                                          .Where(Directory.Exists)
                                          .SelectMany(Directory.EnumerateDirectories)
                                          .Where(dirName => !dirName.Contains(PlayerExporter.ExportFolderName, StringComparison.OrdinalIgnoreCase));

            // Add Built-in packages as projects
            if (includeBuiltInAsProjects)
            {
                projectSearchDirectories = projectSearchDirectories.Concat(Directory.EnumerateDirectories(Path.Combine(T3ParentDirectory, "Operators"))
                                                                                    .Where(path =>
                                                                                           {
                                                                                               var subDir = Path.GetFileName(path);
                                                                                               return
                                                                                                   !subDir
                                                                                                      .StartsWith('.'); // ignore things like .git and file sync folders 
                                                                                           }));
            }
            return projectSearchDirectories;
        }
    }


    private static readonly string CoreOperatorDirectory = Path.Combine(RuntimeAssemblies.CoreDirectory, "Operators");
    private static readonly string T3ParentDirectory = Path.Combine(RuntimeAssemblies.CoreDirectory, "..", "..", "..", "..");
}