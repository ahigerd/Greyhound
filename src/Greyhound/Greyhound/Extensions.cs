using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Greyhound
{
    public static class BinaryReaderExtensions
    {
        public static T ReadStruct<T>(this BinaryReader reader) where T : unmanaged
        {
            unsafe
            {
                var buffer = reader.ReadBytes(sizeof(T));

                T x = new T();

                fixed (byte* p = buffer)
                {
                    return *(T*)p;
                }
            }
        }

        public static T[] ReadArray<T>(this BinaryReader reader, int size) where T : unmanaged
        {
            unsafe
            {
                var sizeOf = sizeof(T);
                var buffer = reader.ReadBytes(sizeOf * size);
                var result = new T[size];

                fixed (T* a = result)
                fixed (byte* b = buffer)
                {
                    for (int i = 0, offset = 0; i < size; i++, offset += sizeOf)
                        a[i] = *(T*)(b + offset);
                }

                return result;
            }
        }
    }
}
