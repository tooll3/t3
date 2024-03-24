using System.Drawing;
using System.Numerics;

namespace T3.SystemUi;

public interface ICoreSystemUiService
{
    public void OpenWithDefaultApplication(string uri);
    public void ShowMessageBox(string text, string title);
    public PopUpResult ShowMessageBox(string text, string title, PopUpButtons buttons);
    public void ShowMessageBox(string message);
    public void ExitApplication();
    public void ExitThread();
    
    public ICursor Cursor { get; }
    void SetUnhandledExceptionMode(bool throwException);
}