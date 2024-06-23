using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using T3.Core.Operator;
using T3.Core.Utils;

namespace T3.Core.Resource;

public sealed partial class ResourceManager
{
    public static IEnumerable<IResourcePackage> GetSharedPackagesForFilters(string[] fileExtensionFilters, bool isFolder, out string[] culledFilters)
    {
        // if the package is read-only and has no files that match any of the filters, it should not be included
        
        CullFilters(isFolder, ref fileExtensionFilters, out var filterAcceptsShaders, out var shaderFilters);
        var packages = SharedResourcePackages
           .Where(package => !package.IsReadOnly || AllMatchingPathsIn(package, false, PathMode.Raw, fileExtensionFilters).Any());
        
        if (filterAcceptsShaders && shaderFilters.Length > 0)
        {
            // we don't need to check for read-only packages here as any package that is in ShaderPackages but not SharedResourcePackages will be read-only
            packages = packages
               .Concat(ShaderPackages.Except(SharedResourcePackages)
                                     .Where(package => AllMatchingPathsIn(package, false, PathMode.Raw, shaderFilters).Any()));
        }
        
        culledFilters = fileExtensionFilters;
        return packages;
    }
    
    public static IEnumerable<string> EnumerateResources(string[] fileExtensionFilters, bool isFolder, IEnumerable<IResourcePackage> packages,
                                                         PathMode pathMode)
    {
        CullFilters(isFolder, ref fileExtensionFilters, out var filterAcceptsShaders, out var shaderFilters);
        
        var allFiles = packages
                      .Concat(SharedResourcePackages)
                      .Distinct()
                      .SelectMany(x => AllMatchingPathsIn(x, isFolder, pathMode, fileExtensionFilters));
        
        if (filterAcceptsShaders && shaderFilters.Length > 0)
        {
            allFiles = allFiles.Concat(ShaderPackages.Except(SharedResourcePackages)
                                                     .SelectMany(x => AllMatchingPathsIn(x, isFolder, pathMode, shaderFilters)));
        }
        
        
        // handle always-shared shaders
        return !isFolder && filterAcceptsShaders
                   ? allFiles.Concat(SharedShaderPackages
                                    .Except(SharedResourcePackages)
                                    .SelectMany(x => AllMatchingPathsIn(x, false, pathMode, shaderFilters)))
                   : allFiles;
    }
    
    private static void CullFilters(bool isFolder, ref string[] fileExtensionFilter, out bool filterAcceptsShaders, out string[] shaderFilters)
    {
        const string shaderExtension = ".hlsl";
        bool mightAcceptShaders = !isFolder;
        
        // the result of assigning this here is that even if a the requested filters are only wildcards, shaders will not show up unless explicitly requested
        // this is probably the desired behavior in most scenarios and can enforce a safer workflow as shaders could otherwise end up being
        // read for file IO when they're in a readonly package
        shaderFilters = mightAcceptShaders ? fileExtensionFilter.Where(x => x.EndsWith(shaderExtension)).ToArray() : [];
        
        // if there's an "all" wildcard, all other filters are irrelevant
        if (fileExtensionFilter.Length == 0 || fileExtensionFilter.Length > 0 && (fileExtensionFilter.Contains("*.*") || fileExtensionFilter.Contains("*")))
        {
            fileExtensionFilter = isFolder ? ["*"] : ["*.*"];
        }
        
        filterAcceptsShaders = mightAcceptShaders && shaderFilters.Length > 0 || fileExtensionFilter.Any(x => x.EndsWith('*'));
    }
    
    private static IEnumerable<string> AllMatchingPathsIn(IResourcePackage package, bool useFolder, PathMode pathMode, string[] filters)
    {
        Func<string, string, SearchOption, IEnumerable<string>> searchFunc = useFolder
                                                                                 ? Directory.EnumerateDirectories
                                                                                 : Directory.EnumerateFiles;
        
        foreach (var path in searchFunc(package.ResourcesFolder, "*", SearchOption.AllDirectories))
        {
            var pathSpan = path.AsSpan();
            var lastSlashIndex = pathSpan.LastIndexOf(Path.DirectorySeparatorChar);
            
            var fileNameSpan = lastSlashIndex == -1 ? pathSpan : pathSpan[(lastSlashIndex + 1)..];
            var fileName = fileNameSpan.ToString();
            
            foreach (var filter in filters)
            {
                if (!StringUtils.MatchesSearchFilter(fileName, filter, true))
                    continue;
                
                if (filter.EndsWith('*'))
                {
                    if (PathIsShader(fileName))
                        continue;
                }
                
                // return the path in the requested format
                yield return pathMode switch
                                 {
                                     PathMode.Absolute => path.Replace('\\', '/'),
                                     PathMode.Relative => path[(package.ResourcesFolder.Length + 1)..].Replace('\\', '/'),
                                     PathMode.Aliased  => $"/{package.Alias}/{path.AsSpan()[(package.ResourcesFolder.Length + 1)..]}".Replace('\\', '/'),
                                     PathMode.Raw      => path,
                                     _                 => throw new ArgumentOutOfRangeException(nameof(pathMode), pathMode, null)
                                 };
            }
        }
    }
    
    public const string DefaultShaderFilter = "*.hlsl";
    private static bool PathIsShader(string path) => StringUtils.MatchesSearchFilter(path, DefaultShaderFilter, true);
}

public static class ResourceExtensions
{
    public static IEnumerable<IResourcePackage> PackagesInCommon(this IEnumerable<Instance> instances)
    {
        return instances.Select(x => x.AvailableResourcePackages)
                        .Aggregate<IEnumerable<IResourcePackage>>((a, b) => a.Intersect(b));
    }
}