#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using T3.Core.Compilation;
using T3.Core.Model;
using T3.Core.Resource;
using T3.Core.UserData;
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
        #if DEBUG
        Stopwatch totalStopwatch = new();
        totalStopwatch.Start();
        #endif

        // todo: change to load CsProj files from specific directories and specific nuget packages from a package directory
        ConcurrentBag<AssemblyInformation> nonOperatorAssemblies = [];

        #if !DEBUG
        // Load pre-built built-in packages as read-only
        LoadBuiltInPackages(nonOperatorAssemblies);
        #endif

        // Find project files
        var csProjFiles = FindCsProjFiles();

        // Load projects
        LoadProjects(csProjFiles, nonOperatorAssemblies, forceRecompile, unsatisfiedProjects: out _, failedProjects: out _);

        // Register UI types
        var allPackages = ActivePackages.Values
                                        .ToArray();

        foreach (var assembly in nonOperatorAssemblies)
        {
            if (assembly.IsEditorOnly)
            {
                EditorOnlyPackages.Add(assembly);
            }
        }

        UiRegistration.RegisterUiTypes();

        // Update all symbol packages
        UpdateSymbolPackages(allPackages);

        // Initialize resources and shader linting
        foreach (var package in allPackages)
        {
            InitializePackageResources(package);
        }

        ShaderLinter.AddPackage(SharedResources.ResourcePackage, ResourceManager.SharedShaderPackages);

        // Initialize custom UIs
        InitializeCustomUis(EditorOnlyPackages);

        foreach (var package in SymbolPackage.AllPackages)
        {
            Log.Debug($"Completed loading {package.DisplayName}");
        }

        #if DEBUG
        totalStopwatch.Stop();
        Log.Info($"Total load time: {totalStopwatch.ElapsedMilliseconds}ms");
        #endif
    }

    private static void LoadBuiltInPackages(ConcurrentBag<AssemblyInformation> nonOperatorAssemblies)
    {
        var directory = Directory.CreateDirectory(CoreOperatorDirectory);

        directory
           .EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
           .Where(folder => !folder.Name.EndsWith(FileLocations.ExportFolderName, StringComparison.OrdinalIgnoreCase)) // ignore "player" project directory
           .ToList()
           .ForEach(directoryInfo =>
                    {
                        var thisDir = directoryInfo.FullName;
                        var packageInfo = Path.Combine(thisDir, RuntimeAssemblies.PackageInfoFileName);
                        if (!RuntimeAssemblies.TryLoadAssemblyFromPackageInfoFile(packageInfo, out var assembly, out var releaseInfo))
                        {
                            Log.Error($"Failed to load assembly from package info file at {packageInfo}");
                            return;
                        }

                        if (!assembly.IsEditorOnly)
                        {
                            AddToLoadedPackages(new PackageWithReleaseInfo(new EditorSymbolPackage(assembly), releaseInfo));
                        }
                        else
                        {
                            nonOperatorAssemblies.Add(assembly);
                        }
                    });
    }

    /// <summary>
    /// Load each project file and its associated assembly
    /// </summary>
    [SuppressMessage("ReSharper", "OutParameterValueIsAlwaysDiscarded.Local")]
    private static void LoadProjects(FileInfo[] csProjFiles, ConcurrentBag<AssemblyInformation> nonOperatorAssemblies, bool forceRecompile,
                                     out List<ProjectWithReleaseInfo> unsatisfiedProjects, out List<ProjectWithReleaseInfo> failedProjects)
    {
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

                                  // this may call for some reworking of how that works (MSBuild actions in C#?), or generation at project creation time
                                  if (!csProjFile.TryGetReleaseInfo(out var releaseInfo) && !csProjFile.TryRecompile(out releaseInfo, true))
                                  {
                                      Log.Error($"Failed to load release info for {csProjFile.Name}");
                                      return new ProjectWithReleaseInfo(fileInfo, csProjFile, null);
                                  }

                                  return new ProjectWithReleaseInfo(fileInfo, csProjFile, releaseInfo);
                              })
                      .Where(x =>
                             {
                                 if (x.ReleaseInfo == null)
                                 {
                                     Log.Error($"Failed to load release info for {x.ProjectFile.FullName}");
                                     return false;
                                 }

                                 if (x.CsProject != null) return true;

                                 Log.Error($"Failed to load project at \"{x.ProjectFile.FullName}\"");
                                 return false;
                             })
                      .ToArray();

        LoadProjects(nonOperatorAssemblies, releases, forceRecompile, out failedProjects, out unsatisfiedProjects);

        foreach (var project in failedProjects)
        {
            Log.Error($" Failed to load {project.CsProject!.Name}");
        }

        foreach (var project in unsatisfiedProjects)
        {
            Log.Error($" Unsatisfied dependencies for {project.CsProject!.Name}");
        }
    }

    private static void LoadProjects(ConcurrentBag<AssemblyInformation> nonOperatorAssemblies, IReadOnlyCollection<ProjectWithReleaseInfo> releases,
                                     bool forceRecompile,
                                     out List<ProjectWithReleaseInfo> failedProjects, 
                                     out List<ProjectWithReleaseInfo> unsatisfiedProjects)
    {
        List<ProjectWithReleaseInfo> satisfied = [];
        var unsatisfied = new List<ProjectWithReleaseInfo>();
        foreach (var release in releases)
        {
            if (AllDependenciesAreSatisfied(release))
                satisfied.Add(release);
            else
                unsatisfied.Add(release);
        }

        var failed = new List<ProjectWithReleaseInfo>();
        List<PackageWithReleaseInfo> loadedOperatorPackages = [];

        var shouldTryAgain = true;

        while (shouldTryAgain)
        {
            // empty failures into satisfied
            for (int i = failed.Count - 1; i >= 0; i--)
            {
                var release = failed[i];
                failed.RemoveAt(i);
                satisfied.Add(release);
            }

            // compile all projects that have satisfied dependencies
            CompileSatisfiedPackages();
            // retry failures after any successes
            RetryFailures();

            var unsatisfiedCount = unsatisfied.Count;

            SatisfyDependencies();

            // if no further dependencies were satisfied, we're done
            // whether it's because of success or failure
            shouldTryAgain = unsatisfied.Count < unsatisfiedCount;
        }

        // try compiling/loading the unsatisfied projects anyway. should we do this?
        // todo - this compilation process is not as robust as the previous loop
        for (var index = unsatisfied.Count - 1; index >= 0; index--)
        {
            var project = unsatisfied[index];
            if (!TryLoad(nonOperatorAssemblies, project, loadedOperatorPackages, forceRecompile))
            {
                failed.Add(project);
                unsatisfied.RemoveAt(index);
            }
        }

        unsatisfiedProjects = unsatisfied;
        failedProjects = failed;
        return;

        static bool TryLoad(ConcurrentBag<AssemblyInformation> nonOperatorAssemblies, ProjectWithReleaseInfo release,
                            List<PackageWithReleaseInfo> loadedOperatorPackages, bool forceRecompile)
        {
            if (!TryLoadProject(release, forceRecompile, out var operatorPackage))
            {
                return false;
            }

            if (operatorPackage.HasValue) // won't have value if the assembly is a non-operator assembly
            {
                loadedOperatorPackages.Add(operatorPackage.Value);
                AddToLoadedPackages(operatorPackage.Value);
                return true;
            }

            var assembly = release.CsProject!.Assembly!;
            nonOperatorAssemblies.Add(assembly);
            return true;
        }

        void CompileSatisfiedPackages()
        {
            for (var i = satisfied.Count - 1; i >= 0; i--)
            {
                var release = satisfied[i];
                satisfied.RemoveAt(i);
                if (!TryLoad(nonOperatorAssemblies, release, loadedOperatorPackages, forceRecompile))
                    failed.Add(release);
            }
        }

        void RetryFailures()
        {
            for (var i = failed.Count - 1; i >= 0; i--)
            {
                var release = failed[i];
                failed.RemoveAt(i);
                if (!TryLoad(nonOperatorAssemblies, release, loadedOperatorPackages, forceRecompile))
                    failed.Add(release);
            }
        }

        void SatisfyDependencies()
        {
            for (var i = unsatisfied.Count - 1; i >= 0; i--)
            {
                var release = unsatisfied[i];
                if (AllDependenciesAreSatisfied(release))
                {
                    satisfied.Add(release);
                    unsatisfied.RemoveAt(i);
                }
            }
        }
    }

    private static FileInfo[] FindCsProjFiles()
    {
        return GetProjectDirectories()
              .SelectMany(dir => Directory.EnumerateFiles(dir, "*.csproj", SearchOption.AllDirectories))
              .Select(x => new FileInfo(x))
              .ToArray();
    }

    private static IEnumerable<string> GetProjectDirectories()
    {
        // ReSharper disable once JoinDeclarationAndInitializer
        string[] topDirectories = [UserSettings.Config.ProjectsFolder];

        var projectSearchDirectories = topDirectories
                                      .Where(Directory.Exists)
                                      .SelectMany(Directory.EnumerateDirectories)
                                      .Where(dirName => !dirName.Contains(FileLocations.ExportFolderName, StringComparison.OrdinalIgnoreCase));

        #if DEBUG // Add Built-in packages as projects
        projectSearchDirectories = projectSearchDirectories.Concat(Directory.EnumerateDirectories(Path.Combine(T3ParentDirectory, "Operators"))
                                                                            .Where(path =>
                                                                                   {
                                                                                       var subDir = Path.GetFileName(path);
                                                                                       return
                                                                                           !subDir
                                                                                              .StartsWith('.'); // ignore things like .git and file sync folders 
                                                                                   }));
        #endif
        return projectSearchDirectories;
    }

    private static readonly string CoreOperatorDirectory = Path.Combine(RuntimeAssemblies.CoreDirectory, "Operators");
    private static readonly string T3ParentDirectory = Path.Combine(RuntimeAssemblies.CoreDirectory, "..", "..", "..", "..");
}