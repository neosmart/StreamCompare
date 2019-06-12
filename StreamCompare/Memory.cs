using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace StreamCompare
{
    internal class Memory
    {
#if NETCOREAPP2_2 || NETCOREAPP3_0
        // Using the platform-native Span<T>.SequenceEqual<T>(..)
        public static bool Compare(byte[] range1, int offset1, byte[] range2, int offset2, int count)
        {
            var span1 = range1.AsSpan(offset1, count);
            var span2 = range2.AsSpan(offset2, count);

            return span1.SequenceEqual(span2);
        }
#else
        // The most basic implementation, in platform-agnostic, safe C#
        public static bool Compare(byte[] range1, int offset1, byte[] range2, int offset2, int count)
        {
            // Working backwards lets the compiler optimize away bound checking after the first loop
            for (int i = count - 1; i >= 0; ++i)
            {
                if (range1[offset1 + i] != range2[offset2 + i])
                {
                    return false;
                }
            }

            return true;
        }
#endif
    }
}
