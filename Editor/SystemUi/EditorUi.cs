using T3.Core.SystemUi;
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
                throw new Exception($"{typeof(EditorUi)}'s {nameof(Instance)} already set to {_instance.GetType()}");
            
            _instance = value;
            CoreUi.Instance = value;
        }
    }
}