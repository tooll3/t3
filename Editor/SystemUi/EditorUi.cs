using Microsoft.VisualBasic.ApplicationServices;
using System.Windows.Forms;
using T3.SystemUi;

namespace T3.Editor.SystemUi;

public static class EditorUi
{
    private static IEditorSystemUiService _instance;
    public static IEditorSystemUiService Instance
    {
        get => _instance;
        set
        {
            if (_instance != null)
                throw new CantStartSingleInstanceException($"{typeof(EditorUi)}'s {nameof(Instance)} already set to {_instance.GetType()}");
            
            _instance = value;
        }
    }
    public static Screen[] AllScreens {
        get => Screen.AllScreens;
    }
}