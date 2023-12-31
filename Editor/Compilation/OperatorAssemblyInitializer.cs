using System;
using System.IO;
using System.Linq;
using System.Reflection;
using SharpDX.Direct2D1;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Resource;

namespace T3.Core.Compilation;

public static class EditorAssemblyInfo
{
    public static readonly Assembly Core = typeof(ResourceManager).Assembly;
    public static readonly Assembly CoreEditor = typeof(EditorAssemblyInfo).Assembly;
    private static readonly string BinaryDirectory = Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar;
    private static Assembly[] _operatorAssemblies;

    public static Assembly[] GetAllOperatorAssemblies()
    {
        return _operatorAssemblies ??= LoadOperatorAssemblies();

        // todo - this is a hack to get the operator assemblies without referencing the editor
        // there should probably be a better way to do this
    }

    private static Assembly[] LoadOperatorAssemblies()
    {
        Log.Debug($"Attempting to load operator assemblies...");
        var currentAssemblyFullNames = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetName().FullName).ToList();
        var coreAssemblyName = Core.GetName();
        var editorAssemblyName = CoreEditor.GetName();

        var absolutePath = Path.GetFullPath(SymbolData.OperatorDirectoryName);
        var workingDirectory = Directory.GetCurrentDirectory();

        try
        {
            var assemblies = Directory.GetFiles(workingDirectory, "*.dll", SearchOption.AllDirectories)
                                       //.Where(path => path.Contains(BinaryDirectory))
                                      .Where(path => !path.Contains("NDI") && !path.Contains("Spout"))
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
                                                        && assemblyName.ProcessorArchitecture == coreAssemblyName.ProcessorArchitecture
                                                        && !currentAssemblyFullNames.Contains(assemblyName.FullName);
                                             })
                                      .Select(name =>
                                              {
                                                  Log.Debug($"Attempting to load assembly at {name.Path}");
                                                  try
                                                  {
                                                      var assembly = Assembly.LoadFile(name.Path);
                                                      currentAssemblyFullNames.Add(name.AssemblyName.FullName);
                                                      Log.Debug($"Loaded assembly {name.AssemblyName.FullName}");
                                                      return assembly;
                                                  }
                                                  catch (Exception e)
                                                  {
                                                      Log.Error($"Failed to load assembly {name.AssemblyName.FullName}\n{name.Path}\n{e.Message}\n{e.StackTrace}");
                                                      return null;
                                                  }
                                              })
                                      .Where(assembly =>
                                             {
                                                 // this should be unnecessary, but it's a safeguard?
                                                 return assembly != null
                                                        && assembly.GetReferencedAssemblies()
                                                                   .All(asmName => asmName.FullName != editorAssemblyName.FullName);
                                             }).ToArray();
            
            Log.Debug($"Loaded operator assemblies: {assemblies.Length}");
            var opAssemblies = assemblies.Where(assembly =>
                                          {
                                              try
                                              {
                                                  return assembly.DefinedTypes.Any(type => type.IsAssignableTo(typeof(Instance)));
                                              }
                                              catch (Exception e)
                                              {
                                                  Log.Error($"Failed to get defined types for {assembly.FullName}\n{e.Message}\n{e.StackTrace}");
                                                  return false;
                                              }
                                          })
                                   .ToArray();
            
            Log.Debug($"Loaded operator assemblies: {opAssemblies.Length}");
            return opAssemblies;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load operator assemblies:\n{e}");
            return Array.Empty<Assembly>();
        }
    }

    private struct AssemblyNameAndPath
    {
        public AssemblyName AssemblyName;
        public string Path;
    }
}