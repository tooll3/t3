using System.Drawing;
using System.Numerics;

namespace T3.SystemUi;

public interface ICoreSystemUiService 
{
    public void ShowMessageBox(string text, string title);
    public PopUpResult ShowMessageBox(string text, string title, PopUpButtons buttons);
    public void ShowMessageBox(string message);
    public void ExitApplication();
    public void ExitThread();
    
    public ICursor Cursor { get; }
    void SetUnhandledExceptionMode(bool throwException);
}

public interface ICursor
{
    public Vector2 PositionVec => new(Position.X, Position.Y);
    public Point Position { get; }
    public void SetVisible(bool visible);
}