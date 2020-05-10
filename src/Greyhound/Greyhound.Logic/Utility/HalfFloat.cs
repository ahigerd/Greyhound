using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Greyhound.Logic
{
    public struct HalfFloat
    {
        [StructLayout(LayoutKind.Explicit)]
        private struct uif
        {
            [FieldOffset(0)]
            public float f;
            [FieldOffset(0)]
            public int i;
            [FieldOffset(0)]
            public uint u;

            public uif(uint v)
            {
                f = 0;
                i = 0;
                u = v;
            }
        }

        public ushort Value { get; set; }

        public unsafe float ToSingle()
        {
            var Mantissa = (uint)(Value & 0x03FF);

            var Exponent = (uint)(Value & 0x7C00);
            if (Exponent == 0x7C00) // INF/NAN
            {
                Exponent = 0x8f;
            }
            else if (Exponent != 0)  // The value is normalized
            {
                Exponent = (uint)(((int)Value >> 10) & 0x1F);
            }
            else if (Mantissa != 0)     // The value is denormalized
            {
                // Normalize the value in the resulting float
                Exponent = 1;

                do
                {
                    Exponent--;
                    Mantissa <<= 1;
                } while ((Mantissa & 0x0400) == 0);

                Mantissa &= 0x03FF;
            }
            else                        // The value is zero
            {
                Exponent = unchecked((uint)(-112));
            }

            var Result =
                (((uint)(Value) & 0x8000) << 16) // Sign
                | ((Exponent + 112) << 23)                      // Exponent
                | (Mantissa << 13);

            return new uif(Result).f;
        }
    }
}
