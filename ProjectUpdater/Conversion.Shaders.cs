using T3.Core.Resource;

namespace ProjectUpdater;

internal sealed partial class Conversion
{
    private FileChangeInfo ConvertShaderFile(string filePath, string fileContents, string originalRootDirectory, string newRootDirectory)
    {
        var relativePath = Path.GetRelativePath(originalRootDirectory, filePath);
        var newPath = Path.Combine(newRootDirectory, ResourceManager.ResourcesSubfolder, relativePath);
        fileContents = fileContents.Replace("lib/", "")
                                   .Replace(@"lib\\", "");
        return new FileChangeInfo(newPath, fileContents.Replace("lib/shared", "shared"));
    }
}