using T3.SystemUi;
namespace T3.MsForms;

public class MsFormsEditor : MsForms, IEditorSystemUiService
{
    void IEditorSystemUiService.EnableDpiAwareScaling()
    {
        Application.EnableVisualStyles();
        Application.SetHighDpiMode(HighDpiMode.PerMonitor);
        Application.SetCompatibleTextRenderingDefault(false);
    }

    void IEditorSystemUiService.SetClipboardText(string text)
    {
        try
        {
            Clipboard.SetText(text, TextDataFormat.UnicodeText);
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            // TODO: should log this 
        }
    }

    string IEditorSystemUiService.GetClipboardText()
    {
        return Clipboard.GetText();
    }

    public string StartupPath => Application.StartupPath;
    IFilePicker IEditorSystemUiService.CreateFilePicker()
    {
        return new OpenFileDialogWrapper();
    }

    private sealed class OpenFileDialogWrapper : IFilePicker
    {
        private readonly OpenFileDialog _dialog;
        internal OpenFileDialogWrapper()
        {
            _dialog = new();
        }
        
        public void Dispose()
        {
            _dialog.Dispose();
        }

        public string FileName { get => _dialog.FileName; set => _dialog.FileName = value; }
        public string Filter
        {
            get => _dialog.Filter;
            set
            {
                try
                {
                    _dialog.Filter = value;
                }
                catch (ArgumentException e)
                {
                    // TODO: should log this
                }
            }
        }

        public string InitialDirectory { get => _dialog.InitialDirectory; set => _dialog.InitialDirectory = value; }
        public bool Multiselect { get => _dialog.Multiselect; set => _dialog.Multiselect = value; }
        public bool RestoreDirectory { get => _dialog.RestoreDirectory; set => _dialog.RestoreDirectory = value; }
        public bool ShowHelp { get => _dialog.ShowHelp; set => _dialog.ShowHelp = value; }
        public bool ShowReadOnly { get => _dialog.ShowReadOnly; set => _dialog.ShowReadOnly = value; }
        public string Title { get => _dialog.Title; set => _dialog.Title = value; }
        public bool ValidateNames { get => _dialog.ValidateNames; set => _dialog.ValidateNames = value; }
        public PopUpResult ShowDialog()
        {
            var result = _dialog.ShowDialog();
            return ResultEnumConversion[result];
        }

        public bool CheckFileExists { get => _dialog.CheckFileExists; set => _dialog.CheckFileExists = value; }
        public bool CheckPathExists { get => _dialog.CheckPathExists; set => _dialog.CheckPathExists = value; }
        public int FilterIndex { get => _dialog.FilterIndex; set => _dialog.FilterIndex = value; }
    }
}