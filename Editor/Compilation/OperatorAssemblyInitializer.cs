using System;
using System.IO;
using System.Linq;
using System.Reflection;
using SharpDX.Direct2D1;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;

namespace T3.Core.Compilation;

public static class CoreAssembly
{
    public static readonly Assembly Assembly = typeof(CoreAssembly).Assembly;
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
        var currentAssemblyFullNames = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetName().FullName).ToList();
        var coreAssemblyName = Assembly.GetName();
        
        var absolutePath = Path.GetFullPath(SymbolData.OperatorDirectoryName);

        try
        {
            var assemblies = Directory.GetFiles(absolutePath, "*.dll", SearchOption.AllDirectories)
                                      .Where(path => path.Contains(BinaryDirectory) && path.Contains("net6.0-windows"))
                                      .Select(path => new AssemblyNameAndPath()
                                                          {
                                                              AssemblyName = AssemblyName.GetAssemblyName(path),
                                                              Path = path
                                                          })
                                      .Where(nameAndPath =>
                                             {
                                                 var assemblyName = nameAndPath.AssemblyName;
                                                 return assemblyName.ProcessorArchitecture == coreAssemblyName.ProcessorArchitecture
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
                                                 return assembly != null && !assembly.GetReferencedAssemblies()
                                                                                     .Any(name => name.FullName.Contains("T3.Editor"));
                                             })
                                      .Where(assembly => assembly.DefinedTypes.Any(type => type.IsSubclassOf(typeof(Instance<>))))
                                      .ToArray();

            return assemblies;
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