using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Protocols.Tds;

internal static class ThrowHelper
{
    public static void ThrowIfFalse(
#if NET
        [DoesNotReturnIf(true)]
#endif
        bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("TDS parsing failed");
        }
    }
}
