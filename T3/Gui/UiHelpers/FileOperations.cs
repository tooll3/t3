using System.Windows.Forms;

namespace T3.Gui.UiHelpers
{
    public static class FileOperations
    {
        public static string PickResourceFilePath()
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = GetAbsoluteResourcePath();
                openFileDialog.Filter = "jpg files (*.jpg)|*.jpg|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() != DialogResult.OK)
                    return null;

                var absolutePath = openFileDialog.FileName;
                return ConvertToRelativeFilepath(absolutePath);
            }
        }

        public static string PickResourceDirectory()
        {
            using (var folderBrowser = new OpenFileDialog())
            {
                folderBrowser.InitialDirectory = GetAbsoluteResourcePath();
                folderBrowser.ValidateNames = false;
                folderBrowser.CheckFileExists = false;
                folderBrowser.CheckPathExists = true;
                folderBrowser.FileName = "Folder Selection.";
                if (folderBrowser.ShowDialog() != DialogResult.OK)
                    return null;
                
                var absoluteFolderPath = System.IO.Path.GetDirectoryName(folderBrowser.FileName);
                return ConvertToRelativeFilepath(absoluteFolderPath);
            }
        }
        
        private static string ConvertToRelativeFilepath(string absoluteFilePath)
        {
            var currentApplicationPath = System.IO.Path.GetFullPath(".");
            var firstCharUppercase = currentApplicationPath.Substring(0, 1).ToUpper();
            currentApplicationPath = firstCharUppercase + currentApplicationPath.Substring(1, currentApplicationPath.Length - 1) + "\\";
            var relativeFilePath = absoluteFilePath.Replace(currentApplicationPath, "").Replace("\\", "/");
            return relativeFilePath;
        }

        private static string GetAbsoluteResourcePath()
        {
            return System.IO.Path.Combine(System.IO.Path.GetFullPath("."), "Resources");
        }
    }
}