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
    public T Show<T>(string text, string title, Func<T, string> toString, params T[] buttons);
    public string Show(string text, string title, params string[] buttons) => Show(text, title, str => str, buttons);
    public void Show(string text, string title);
    public void Show(string message);
}

public readonly record struct FontPack(TtfFont Regular, TtfFont Bold, TtfFont Small, TtfFont Large);
public readonly record struct TtfFont(string Path, float PixelSize);
