using System.Numerics;

namespace SilkWindows;

internal interface IImguiDrawer
{
    public void OnRender(string windowName, double deltaSeconds, WindowHandler.Fonts? fonts);

    public void OnWindowUpdate(double deltaSeconds, out bool shouldClose);

    public void OnClose();
}