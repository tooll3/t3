using System.Diagnostics;

namespace T3.Editor.Compilation;

public static class Compiler
{
    public static bool TryCompile(CsProjectFile projectFile)
    {
        // TODO// build destinationDirectory/name.csproj with dotnet build
        var process = new Process
                          {
                              StartInfo = new ProcessStartInfo
                                              {
                                                  FileName = "dotnet",
                                                  Arguments = $"build {projectFile.FileName}",
                                                  WorkingDirectory = projectFile.Directory,
                                                  UseShellExecute = true
                                              }
                          };

        process.Start();
        process.WaitForExit();

        return process.ExitCode == 0; // Todo: need to check for errors - this is a stupid stopgap
    }
}