using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace StreamCompare
{
    internal class Memory
    {
        // The most basic implementation, in platform-agnostic, safe C#
        public static bool Compare(byte[] range1, int offset1, byte[] range2, int offset2, int count)
        {
            Debug.Assert(range1.Length == range2.Length);

            for (int i = 0; i < count; ++i)
            {
                if (range1[offset1 + i] != range2[offset2 + i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
