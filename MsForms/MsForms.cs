using System.Diagnostics;
using Microsoft.VisualBasic.Logging;
using T3.SystemUi;

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter

namespace T3.MsForms;

public class MsForms : ICoreSystemUiService
{
    public MsForms()
    {
    }

    public void SetUnhandledExceptionMode(bool throwException)
    {
        System.Windows.Forms.Application.SetUnhandledExceptionMode(throwException
                                                                       ? UnhandledExceptionMode.ThrowException
                                                                       : UnhandledExceptionMode.CatchException);
    }

    void ICoreSystemUiService.OpenWithDefaultApplication(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new Exception("Uri is empty");
        }

        var startInfo = new ProcessStartInfo
                            {
                                FileName = "cmd",
                                Arguments = $"/c start {uri}",
                            };

        Process.Start(startInfo);
    }

    void ICoreSystemUiService.ShowMessageBox(string text, string title)
    {
        MessageBox.Show(text, title);
    }

    PopUpResult ICoreSystemUiService.ShowMessageBox(string text, string title, PopUpButtons buttons)
    {
        DialogResult result = MessageBox.Show(text, title, ButtonEnumConversion[buttons]);
        return ResultEnumConversion[result];
    }

    void ICoreSystemUiService.ShowMessageBox(string message)
    {
        MessageBox.Show(message);
    }

    void ICoreSystemUiService.ExitApplication()
    {
        if (System.Windows.Forms.Application.MessageLoop)
        {
            // Use this since we are a WinForms app
            System.Windows.Forms.Application.Exit();
        }
        else
        {
            // Use this since we are a console app
            System.Environment.Exit(1);
        }
    }

    void ICoreSystemUiService.ExitThread()
    {
        Application.ExitThread();
    }

    public ICursor Cursor => FirstCursor ??= new CursorWrapper();

    private static readonly Dictionary<PopUpButtons, MessageBoxButtons> ButtonEnumConversion =
        new()
            {
                { PopUpButtons.Ok, MessageBoxButtons.OK },
                { PopUpButtons.OkCancel, MessageBoxButtons.OKCancel },
                { PopUpButtons.AbortRetryIgnore, MessageBoxButtons.AbortRetryIgnore },
                { PopUpButtons.YesNoCancel, MessageBoxButtons.YesNoCancel },
                { PopUpButtons.YesNo, MessageBoxButtons.YesNo },
                { PopUpButtons.RetryCancel, MessageBoxButtons.RetryCancel },
                { PopUpButtons.CancelTryContinue, MessageBoxButtons.CancelTryContinue },
            };

    protected static readonly Dictionary<DialogResult, PopUpResult> ResultEnumConversion =
        new()
            {
                { DialogResult.None, PopUpResult.None },
                { DialogResult.OK, PopUpResult.Ok },
                { DialogResult.Cancel, PopUpResult.Cancel },
                { DialogResult.Abort, PopUpResult.Abort },
                { DialogResult.Retry, PopUpResult.Retry },
                { DialogResult.Ignore, PopUpResult.Ignore },
                { DialogResult.Yes, PopUpResult.Yes },
                { DialogResult.No, PopUpResult.No },
                { DialogResult.TryAgain, PopUpResult.TryAgain },
                { DialogResult.Continue, PopUpResult.Continue }
            };

    public static void TrackKeysOf(Form form)
    {
        form.KeyDown += HandleKeyDown;
        form.KeyUp += HandleKeyUp;
    }

    public static void TrackMouseOf(Form form)
    {
        var cursorWrapper = new CursorWrapper();
        cursorWrapper.TrackMouseOf(form);

        if (TrackedCursors.Count == 0)
            FirstCursor = cursorWrapper;
        
        TrackedCursors.Add(form, cursorWrapper);
    }

    private static void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        var keyIndex = (int)e.KeyCode;
        KeyHandler.SetKeyDown(keyIndex);
    }

    private static void HandleKeyUp(object? sender, KeyEventArgs e)
    {
        var keyIndex = (int)e.KeyCode;
        KeyHandler.SetKeyUp(keyIndex);
    }

    internal static ICursor? FirstCursor;
    private static readonly Dictionary<Form, CursorWrapper> TrackedCursors = new();
}