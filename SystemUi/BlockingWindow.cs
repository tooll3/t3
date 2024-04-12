using T3.SystemUi;

namespace T3.Core.SystemUi;

public static class BlockingWindow
{
    private static IPopUpWindowProvider _instance;
    public static IPopUpWindowProvider Instance
    {
        get => _instance;
        set
        {
            if (_instance != null)
                throw new Exception($"{typeof(BlockingWindow)}'s {nameof(Instance)} already set to {_instance.GetType()}");
            
            _instance = value;
        }
    }
}