namespace Assets.Voxelmetric.Code.Common.Collections
{
    // ----------------------------------------------------------------------------
    // Tuple structs for use in .NET Not-Quite-3.5 (e.g. Unity3D).
    //
    // Used Chapter 3 in http://functional-programming.net/ as a starting point.
    //
    // Note: .NET 4.0 Tuples are immutable classes so they're *slightly* different.
    // ----------------------------------------------------------------------------

    /// <summary>
    /// Utility class that simplifies cration of tuples by using
    /// method calls instead of constructor calls
    /// </summary>
    public static class Tuple
    {
        /// <summary>
        /// Creates a new tuple value with the specified elements. The method
        /// can be used without specifying the generic parameters, because C#
        /// compiler can usually infer the actual types.
        /// </summary>
        /// <param name="item1">First element of the tuple</param>
        /// <param name="second">Second element of the tuple</param>
        /// <returns>A newly created tuple</returns>
        public static Tuple<T1, T2> Create<T1, T2>(T1 item1, T2 second)
        {
            return new Tuple<T1, T2>(item1, second);
        }

        /// <summary>
        /// Extension method that provides a concise utility for unpacking
        /// tuple components into specific out parameters.
        /// </summary>
        /// <param name="tuple">the tuple to unpack from</param>
        /// <param name="ref1">the out parameter that will be assigned tuple.Item1</param>
        /// <param name="ref2">the out parameter that will be assigned tuple.Item2</param>
        public static void Unpack<T1, T2>(this Tuple<T1, T2> tuple, out T1 ref1, out T2 ref2)
        {
            ref1 = tuple.Item1;
            ref2 = tuple.Item2;
        }
    }
}
