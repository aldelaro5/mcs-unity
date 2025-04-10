#if NET35

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

namespace System.Runtime.CompilerServices {
    /// <summary>
    /// This interface is required for types that want to be indexed into by dynamic patterns.
    /// </summary>
    internal interface ITuple {
        /// <summary>
        /// The number of positions in this data structure.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Get the element at position <param name="index"/>.
        /// </summary>
        object? this[int index] { get; }
    }
}
#endif