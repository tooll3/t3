namespace SilkWindows;

public static class ImguiWindowService
{
    private static IImguiWindowProvider? _instance;
    
    public static IImguiWindowProvider Instance
    {
        get => _instance!;
        set
        {
            if (_instance != null)
                throw new Exception($"{typeof(ImguiWindowService)}'s {nameof(Instance)} already set to {_instance.GetType()}");
            
            _instance = value;
        }
    }
}