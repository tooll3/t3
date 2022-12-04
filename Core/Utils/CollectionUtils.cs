namespace T3.Core.Utils
{
    public static class CollectionUtils
    {
        /// <summary>
        /// A simple method to get a new index by "jumping" from a startIndex
        /// When the jump would make the index exceed the valid range of the collection,
        /// it will return an index that "wraps" around the other side
        /// </summary>
        /// <param name="startIndex">Index to jump from</param>
        /// <param name="jumpAmount">How far to jump (usually 1 or -1 for forward and backward respectively)</param>
        /// <param name="collection">The collection whose Count (Length for arrays) will be referenced</param>
        /// <param name="realWrap">If false, this will default to making every wrap result in the first or last index of the collection.
        /// If true, it will be a "real" wrap, where if you jump in excess of X, you will wrap to the index X distance from the other side. </param>
        /// <returns></returns>
        public static int WrapIndex(int startIndex, int jumpAmount, System.Collections.IList collection, bool realWrap = false)
        {
            int index = startIndex + jumpAmount;
            index %= collection.Count;

            bool newIndexIsNegative = index < 0;
            int negativeWrapIndex = realWrap ? collection.Count + index : collection.Count - 1;

            return newIndexIsNegative ? negativeWrapIndex : index;
        }
    }
}
