using Microsoft.VisualBasic.ApplicationServices;

namespace T3.Editor.SystemUi;

internal static class EditorUi
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
}