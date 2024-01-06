using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualBasic.ApplicationServices;
using T3.Core.Logging;
using T3.Core.Operator;

namespace T3.Core.Compilation;

public class RuntimeAssemblies
{
    public static readonly AssemblyInformation Core;
    public static IReadOnlyList<AssemblyInformation> AllAssemblies { get; private set; }
    public static IReadOnlyList<AssemblyInformation> DynamicallyLoadedAssemblies { get; private set; }
    public static IReadOnlyList<AssemblyInformation> OperatorAssemblies { get; private set; }

    static RuntimeAssemblies()
    {
        var coreAssembly = typeof(RuntimeAssemblies).Assembly;
        var coreAssemblyName = coreAssembly.GetName();
        var path = coreAssembly.Location;
        Core = new AssemblyInformation(path, coreAssemblyName, coreAssembly);
        var baseAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                                      .Where(x => !x.IsDynamic)
                                      .Where(x => !x.GetName().FullName.Contains("CodeAnalysis")) // Only the editor needs CodeAnalysis 
                                      .Select(x => new AssemblyInformation(x.Location, x.GetName(), x))
                                      .ToArray();

        var workingDirectory = Directory.GetCurrentDirectory();
        DynamicallyLoadedAssemblies = LoadAssemblies(baseAssemblies, workingDirectory);

        OperatorAssemblies = DynamicallyLoadedAssemblies
                            .Where(AssemblyContainsOperators)
                            .ToArray();

        Log.Debug($"Loaded {DynamicallyLoadedAssemblies.Count} assemblies.");
        Log.Debug($"Loaded {OperatorAssemblies.Count} operator assemblies.");

        AllAssemblies = DynamicallyLoadedAssemblies
                       .Concat(baseAssemblies)
                       .ToArray();
    }

    private static bool AssemblyContainsOperators(AssemblyInformation assemblyInformation)
    {
        try
        {
            return assemblyInformation.Assembly.DefinedTypes
                                      .Any(typeInfo => typeInfo.IsAssignableTo(typeof(Instance)));
        }
        catch (Exception e)
        {
            Log.Debug($"Failed to check if assembly '{assemblyInformation.Name}' ({assemblyInformation.Path}) " +
                      $"contains operator types.\n{e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    public static IReadOnlyList<AssemblyInformation> LoadNewOperatorAssemblies(string directory)
    {
        var assemblies = LoadAssemblies(AllAssemblies, directory);
        var operatorAssemblies = assemblies.Where(AssemblyContainsOperators).ToArray();

        OperatorAssemblies = OperatorAssemblies.Concat(operatorAssemblies).ToArray();
        AllAssemblies = AllAssemblies.Concat(assemblies).ToArray();
        DynamicallyLoadedAssemblies = DynamicallyLoadedAssemblies.Concat(assemblies).ToArray();
        return operatorAssemblies;
    }

    static AssemblyInformation[] LoadAssemblies(IEnumerable<AssemblyInformation> baseAssemblies, string directory)
    {
        Log.Debug($"Attempting to load operator assemblies...");
        var currentAssemblyFullNames = baseAssemblies.Select(x => x.AssemblyName.FullName).ToList();


        try
        {
            return Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories)
                             //.Where(path => path.Contains(BinaryDirectory))
                             //.Where(path => !path.Contains("NDI") && !path.Contains("Spout"))
                            .Select(path =>
                                    {
                                        AssemblyName assemblyName = null;
                                        try
                                        {
                                            assemblyName = AssemblyName.GetAssemblyName(path);
                                        }
                                        catch (Exception e)
                                        {
                                            Log.Debug($"Failed to get assembly name for {path}\n{e.Message}\n{e.StackTrace}");
                                        }

                                        return new AssemblyNameAndPath()
                                                   {
                                                       AssemblyName = assemblyName,
                                                       Path = path
                                                   };
                                    })
                            .Where(nameAndPath =>
                                   {
                                       var assemblyName = nameAndPath.AssemblyName;
                                       return assemblyName != null
                                              && assemblyName.ProcessorArchitecture == Core.AssemblyName.ProcessorArchitecture
                                              && !currentAssemblyFullNames.Contains(assemblyName.FullName)
                                              && !assemblyName.FullName.Contains("CodeAnalysis"); // Only the editor needs CodeAnalysis
                                   })
                            .Select(name =>
                                    {
                                        Log.Debug($"Attempting to load assembly at {name.Path}");
                                        var success = TryLoadAssemblyInformation(name, out var info);
                                        if (success)
                                            currentAssemblyFullNames.Add(info.AssemblyName.FullName);
                                        return info;
                                    })
                            .Where(info => info != null)
                            .ToArray();
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load assemblies\n{e.Message}\n{e.StackTrace}");
            return Array.Empty<AssemblyInformation>();
        }
    }
    
    public static bool TryLoadAssemblyInformation(string path, out AssemblyInformation info)
    {
        AssemblyName assemblyName;
        try
        {
            assemblyName = AssemblyName.GetAssemblyName(path);
        }
        catch(Exception e)
        {
            Log.Error($"Failed to get assembly name for {path}\n{e.Message}\n{e.StackTrace}");
            info = null;
            return false;
        }

        var assemblyNameAndPath = new AssemblyNameAndPath()
                                      {
                                          AssemblyName = assemblyName,
                                          Path = path
                                      };
        
        return TryLoadAssemblyInformation(assemblyNameAndPath, out info);
    }

    private static bool TryLoadAssemblyInformation(AssemblyNameAndPath name, out AssemblyInformation info)
    {
        try
        {
            var assembly = Assembly.LoadFile(name.Path);
            Log.Debug($"Loaded assembly {name.AssemblyName.FullName}");
            info = new AssemblyInformation(name.Path, name.AssemblyName, assembly);
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load assembly {name.AssemblyName.FullName}\n{name.Path}\n{e.Message}\n{e.StackTrace}");
            info = null;
            return false;
        }
    }

    private struct AssemblyNameAndPath
    {
        public AssemblyName AssemblyName;
        public string Path;
    }
}