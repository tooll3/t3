using T3.Core.Operator;

namespace T3.Core.Utils
{
    public static class OperatorUtils
    {

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