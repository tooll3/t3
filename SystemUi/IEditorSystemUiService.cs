namespace T3.SystemUi
{
    public interface IEditorSystemUiService : ICoreSystemUiService
    {
        public void EnableDpiAwareScaling();
        public void SetClipboardText(string text);
        public string GetClipboardText();
        public string StartupPath { get; }
        public IFilePicker CreateFilePicker();
    }

    public interface IFilePicker : IDisposable
    {
        // implements an interface for OpenFileDialog
        public string FileName { get; set; }
        public string Filter { get; set; }
        public string InitialDirectory { get; set; }
        public bool Multiselect { get; set; }
        public bool RestoreDirectory { get; set; }
        public bool ShowHelp { get; set; }
        public bool ShowReadOnly { get; set; }
        public string Title { get; set; }
        public bool ValidateNames { get; set; }
        public PopUpResult ShowDialog();
        public bool CheckFileExists { get; set; }
        public bool CheckPathExists { get; set; }
        public int FilterIndex { get; set; }
    
    }
}