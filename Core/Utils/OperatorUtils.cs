using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using T3.Core.Operator;
using T3.Core.Resource;

namespace T3.Core.Utils
{
    public static class OperatorUtils
    {
        internal static List<Guid> BuildIdPathForInstance(Instance instance)
        {
            var result = new List<Guid>(6);
            while(instance != null)
            {
                result.Insert(0, instance.SymbolChildId);
                instance = instance.Parent;
            }

            return result;
        }
    }
}