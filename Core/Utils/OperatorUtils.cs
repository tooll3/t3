using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core.Operator;

namespace T3.Core.Utils
{
    public static class OperatorUtils
    {
        public static Instance GetInstanceFromIdPath(Instance rootInstance, IReadOnlyCollection<Guid> childPath)
        {
            if (childPath == null || childPath.Count == 0)
                return null;

            var instance = rootInstance;
            foreach (var childId in childPath)
            {
                // Ignore root
                if (childId == rootInstance.SymbolChildId)
                    continue;

                instance = instance.Children.SingleOrDefault(child => child.SymbolChildId == childId);
                if (instance == null)
                    return null;
            }

            return instance;
        }

        public static List<Guid> BuildIdPathForInstance(Instance instance)
        {
            if (instance == null)
                return null;
            
            var result = new List<Guid>(6);
            do
            {
                result.Insert(0, instance.SymbolChildId);
                instance = instance.Parent;
            }
            while (instance != null);

            return result;
        }
    }
}