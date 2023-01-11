using System.Collections.Generic;
using System.Linq;

namespace T3.Core.Logging
{
    public static class RenderStatsCollector
    {
        public static void RegisterProvider(IRenderStatsProvider newProvider)
        {
            if (_providers.Contains(newProvider))
            {
                Log.Warning($"Already registered provider {newProvider}");
                return;
            }
            _providers.Add(newProvider);
        }

        public static void UnregisterProvider(IRenderStatsProvider obsoleteProvider)
        {
            if (!_providers.Contains(obsoleteProvider))
                return;
            
            _providers.Remove(obsoleteProvider);
        }
        
        public static void StartNewFrame()
        {
            ResultsForLastFrame = GetFrameResults().ToList();
            
            foreach (var p in _providers)
            {
                p.StartNewFrame();
            }
        }

        private static IEnumerable<(string, int)> GetFrameResults()
        {
            foreach (var p in _providers)
            {
                foreach (var statAndCount in p.GetStats())
                {
                    yield return statAndCount;
                }
            }
        }

        private static readonly List<IRenderStatsProvider> _providers = new();
        public static List<(string, int)> ResultsForLastFrame { get; private set; }
    }
}