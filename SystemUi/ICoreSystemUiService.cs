using System.Numerics;

namespace T3.SystemUi;

public interface ICoreSystemUiService
{
    public void OpenWithDefaultApplication(string uri);
    public void ExitApplication();
    public void ExitThread();
    
    public ICursor Cursor { get; }
    void SetUnhandledExceptionMode(bool throwException);
}

public interface IPopUpWindows
{
    public void SetFonts(FontPack fontPack);
    public T? ShowMessageBox<T>(string text, string title, Func<T, string> toString, params T[] buttons);
    public string? ShowMessageBox(string text, string title, params string[] buttons) => ShowMessageBox(text, title, str => str, buttons);
    public void ShowMessageBox(string text, string title);
    public void ShowMessageBox(string message);
}

public readonly record struct FontPack(TtfFont Regular, TtfFont Bold, TtfFont Small, TtfFont Large);
public readonly record struct TtfFont(string Path, float PixelSize);
