using T3.Core.Logging;

namespace T3.SystemUi;

public interface ISplashScreen: ILogWriter 
{
    public void Show(string imagePath);
    public void Close();
}