using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Text;

namespace ProjectUpdater;

public static partial class Conversion
{
    private const string DeprecatedNamespacePrefix = "namespace T3.Operators.Types.Id_";
    private const string NamespacePrefix = "namespace ";
    private static readonly CodeDomProvider CodeDomProvider = CodeDomProvider.CreateProvider("C#");

    private static readonly ConcurrentDictionary<Guid, string> DestinationDirectories = new();

    private static readonly List<NamespaceChanged> ChangedNamespaces = [];

    public static void StartConversion(string rootDirectory, string newRootDirectory)
    {
        EnumerateFileOperations(rootDirectory, newRootDirectory, "*.cs", ConvertAndMoveCSharp);
        EnumerateFileOperations(rootDirectory, newRootDirectory, "*.t3", ConvertAndMoveT3File);
        EnumerateFileOperations(rootDirectory, newRootDirectory, "*.t3ui", ConvertAndMoveT3File);

        var changedNamespaces = ChangedNamespaces.ToArray();
        Directory.EnumerateFiles(newRootDirectory, "*.cs", SearchOption.AllDirectories)
            .AsParallel()
            .ForAll(file =>
            {
                using var reader = new StreamReader(file);
                var fileContents = reader.ReadToEnd();
                reader.Close();

                for (var index = 0; index < changedNamespaces.Length; index++)
                {
                    NamespaceChanged namespaceChanged = changedNamespaces[index];
                    fileContents = fileContents.Replace(namespaceChanged.OldNamespace, namespaceChanged.NewNamespace);
                }

                File.WriteAllText(file, fileContents);
            });
    }

    private static FileChangeInfo ConvertAndMoveT3File(string filepath, string fileContents, string originalRootDirectory, string newRootDirectory)
    {
        const string guidKey = "\"Id\": \"";
        var startGuidKeyIndex = fileContents.IndexOf(guidKey, StringComparison.Ordinal);

        if (startGuidKeyIndex == -1)
        {
            Console.WriteLine($"Could not find Id in {filepath}");
            return new FileChangeInfo(filepath, fileContents);
        }
        
        var startGuidIndex = startGuidKeyIndex + guidKey.Length;
        var guidSpan = fileContents.AsSpan(startGuidIndex, 36);
        var gotGuid = Guid.TryParse(guidSpan, out var guid);

        if (!gotGuid)
        {
            Console.WriteLine($"Could not parse guid in \"{filepath}\": \"{guidSpan}\"");
            return FileChangeInfo.Empty;
        }

        if (!DestinationDirectories.TryGetValue(guid, out var destinationDirectory))
        {
            // deduce destination directory from json's "Namespace" field
            const string namespaceKey = "\"Namespace\": \"";
            var namespaceIndex = fileContents.IndexOf(namespaceKey, StringComparison.Ordinal);
            if (namespaceIndex == -1)
            {
                Console.WriteLine($"Could not find Namespace in {filepath}");
                return FileChangeInfo.Empty;
            }
            
            var namespaceStartIndex = namespaceIndex + namespaceKey.Length;
            var namespaceEndIndex = fileContents.IndexOf('\"', namespaceStartIndex);
            var namespaceSpan = fileContents.AsSpan(namespaceStartIndex, namespaceEndIndex - namespaceStartIndex);
            var namespaceSubDirectories = namespaceSpan.ToString().Split('.');
            var subDirs = Path.Combine(namespaceSubDirectories);
            var newDirectory = Path.Combine(newRootDirectory, subDirs);
            var newPath = Path.Combine(newDirectory, Path.GetFileName(filepath));
            
            DestinationDirectories[guid] = newDirectory;            
            return new FileChangeInfo(newPath, fileContents);
        }
        
        var newFilePath = Path.Combine(destinationDirectory, Path.GetFileName(filepath));
        
        return new FileChangeInfo(newFilePath, fileContents);
    }

    private static void EnumerateFileOperations(string rootDirectory, string newRootDirectory, string searchPattern, FileOperationDelegate action)
    {
        Directory.EnumerateFiles(rootDirectory, searchPattern, SearchOption.AllDirectories)
            .AsParallel()
            .ForAll(filePath =>
            {
                var reader = new StreamReader(filePath);
                var fileContents = reader.ReadToEnd();
                reader.Dispose();
                
                var changedFile = action(filePath: filePath, 
                    fileContents: fileContents,  
                    originalRootDirectory: rootDirectory, 
                    newRootDirectory: newRootDirectory);

                if (changedFile == FileChangeInfo.Empty)
                    return;
                
                var directory = Path.GetDirectoryName(changedFile.NewFilePath)!;
                Directory.CreateDirectory(directory);
                try
                {
                    File.WriteAllText(changedFile.NewFilePath, changedFile.NewFileContents);
                }
                catch (IOException e)
                {
                    Console.WriteLine($"Could not write to file: \"{changedFile.NewFilePath}\": {e}");
                    return;
                }
                
                //Console.WriteLine($"Converted {filePath} -> {changedFile.NewFilePath}");

                try
                {
                    File.Delete(filePath);
                }
                catch (IOException e)
                {
                    Console.WriteLine($"Could not delete original file: \"{filePath}\": {e}");
                }
            });
    }

    private static string[] GetSubfolderArray(string filePath, string parentDirectory, StringBuilder sb)
    {
        var fileDirectory = Path.GetDirectoryName(filePath)!;
        var dirWithoutRoot = fileDirectory[(parentDirectory.Length + 1)..];

        for (var index = 0; index < dirWithoutRoot.Length; index++)
        {
            char c = dirWithoutRoot[index];
            if (c == Path.AltDirectorySeparatorChar)
                c = Path.DirectorySeparatorChar;

            sb.Append(c);
        }

        var subfolderComponents = sb.ToString().Split(Path.DirectorySeparatorChar);
        return subfolderComponents;
    }

    private static void ConvertLineEndingsOf(ref string text, bool useWindows)
    {
        if (useWindows)
        {
            text = text
                .Replace("\r\n", "\n")
                .Replace("\n", "\r\n");
        }
        else
        {
            text = text.Replace("\r\n", "\n");
        }
    }

    private delegate FileChangeInfo FileOperationDelegate(string filePath, string fileContents, string originalRootDirectory, string newRootDirectory);

    private readonly struct FileChangeInfo(string newFilePath, string newFileContents)
    {
        private bool Equals(FileChangeInfo other)
        {
            return NewFilePath == other.NewFilePath && NewFileContents == other.NewFileContents;
        }

        public override bool Equals(object? obj)
        {
            return obj is FileChangeInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(NewFilePath, NewFileContents);
        }

        public readonly string NewFilePath = newFilePath;
        public readonly string NewFileContents = newFileContents;
        
        public static readonly FileChangeInfo Empty = new(string.Empty, string.Empty);
        
        // equality operators
        public static bool operator ==(FileChangeInfo left, FileChangeInfo right) => left.NewFilePath == right.NewFilePath 
            && left.NewFileContents == right.NewFileContents;
        
        public static bool operator !=(FileChangeInfo left, FileChangeInfo right) => !(left == right);
    }
}