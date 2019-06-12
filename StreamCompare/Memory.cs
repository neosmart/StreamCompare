using System;

namespace NeoSmart.StreamCompare
{
    internal class Memory
    {
#if (NETCOREAPP2_2 || NETCOREAPP3_0)
        // Using the platform-native Span<T>.SequenceEqual<T>(..)
        public static bool Compare(byte[] range1, int offset1, byte[] range2, int offset2, int count)
        {
            var span1 = range1.AsSpan(offset1, count);
            var span2 = range2.AsSpan(offset2, count);

            return span1.SequenceEqual(span2);
        }
#elif false
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
#else
        // An unsafe implementation that reads 8 bytes at a time
        public static bool Compare(byte[] range1, int offset1, byte[] range2, int offset2, int count)
        {
            if (range1 == range2)
            {
                return true;
            }

            if (range1 is null || range2 is null)
            {
                // If they were both null, the check above would have caught it
                return false;
            }

            unsafe
            {
                fixed (byte *ptr1 = range1)
                fixed (byte *ptr2 = range2)
                {
                    byte *x1 = ptr1 + offset1;
                    byte *x2 = ptr2 + offset2;

                    for (int i = 0; i < (count >> 3); ++i)
                    {
                        if (*((UInt64*)x1) != *((UInt64*)x2))
                        {
                            return false;
                        }

                        x1 += 8;
                        x2 += 8;
                    }

                    if ((count & 4) != 0)
                    {
                        if (*((UInt32*)x1) != *((UInt32*)x2))
                        {
                            return false;
                        }

                        x1 += 4;
                        x2 += 4;
                    }

                    if ((count & 2) != 0)
                    {
                        if (*((UInt16*)x1) != *((UInt16*)x2))
                        {
                            return false;
                        }

                        x1 += 2;
                        x2 += 2;
                    }

                    if ((count & 1) != 0)
                    {
                        if (*((byte*)x1) != *((byte*)x2))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
#endif
    }
}
