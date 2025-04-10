#if NET35
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;

namespace System;

/// <summary>
/// Helper so we can call some tuple methods recursively without knowing the underlying types.
/// </summary>
internal interface ITupleInternal : ITuple {
    string ToString(StringBuilder sb);
    int GetHashCode(IEqualityComparer comparer);
}
#endif