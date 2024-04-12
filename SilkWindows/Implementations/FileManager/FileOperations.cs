namespace SilkWindows.Implementations.FileManager;

internal static class FileOperations
{
    public enum MoveType
    {
        Move,
        Copy
    }
    
    public static bool TryMoveDirectory(MoveType moveType, DirectoryInfo source, DirectoryInfo destination, Func<FileInfo, FileConflictOption> fileConflictHandler)
    {
        if(source.FullName == destination.FullName)
            return true;
        
        var success = false;
        foreach (var file in source.EnumerateFiles())
        {
            success |= TryMoveFile(moveType, file, destination, fileConflictHandler);
        }
        
        foreach (var subDir in source.EnumerateDirectories())
        {
            success |= TryMoveDirectory(moveType, subDir, destination.CreateSubdirectory(subDir.Name), fileConflictHandler);
        }
        
        try
        {
            if (source.GetFileSystemInfos().Length == 0)
                source.Delete();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        
        return success;
    }
    
    public static bool TryMoveFile(MoveType moveType, FileInfo file, DirectoryInfo targetDirectory, Func<FileInfo, FileConflictOption> shouldOverwrite)
    {
        var newFileInfo = new FileInfo(Path.Combine(targetDirectory.FullName, file.Name));
        
        if(newFileInfo.FullName == file.FullName)
            return true;
        
        if (newFileInfo.Exists)
        {
            switch (shouldOverwrite(newFileInfo))
            {
                case FileConflictOption.Skip:
                    Console.WriteLine("File already exists, skipping.");
                    return false;
                case FileConflictOption.Overwrite:
                    break;
                case FileConflictOption.Rename:
                    throw new NotImplementedException(); // todo : create rename window, replace newFileInfo
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        try
        {
            // just in case
            targetDirectory.Create();
            
            switch (moveType)
            {
                case MoveType.Move:
                    file.MoveTo(newFileInfo.FullName, true);
                    break;
                case MoveType.Copy:
                    file.CopyTo(newFileInfo.FullName, true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(moveType), moveType, null);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
        
        return true;
    }
}

internal enum FileConflictOption
{
    Skip,
    Overwrite,
    Rename
}