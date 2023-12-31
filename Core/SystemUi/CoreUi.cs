using Microsoft.VisualBasic.ApplicationServices;
using T3.SystemUi;

namespace T3.Core.SystemUi;

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