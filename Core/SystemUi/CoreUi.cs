using Microsoft.VisualBasic.ApplicationServices;
using T3.SystemUi;

namespace T3.Core.SystemUi;

/// <summary>
/// This is the way to call "system" UI functions, such as pop up windows, etc.
/// It should be set to an instance of <see cref="ICoreSystemUiService"/>, which would be
/// an implementation of Windows Forms, Avalonia, WPF, QT, etc. The non-Imgui UI
/// </summary>
public static class CoreUi
{
    private static ICoreSystemUiService _instance;
    public static ICoreSystemUiService Instance
    {
        get => _instance;
        set
        {
            if (_instance != null)
                throw new CantStartSingleInstanceException($"{typeof(CoreUi)}'s {nameof(Instance)} already set to {_instance.GetType()}");
            
            _instance = value;
        }
    }
}