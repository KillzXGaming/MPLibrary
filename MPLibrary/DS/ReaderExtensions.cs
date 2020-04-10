using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;

namespace MPLibrary.DS
{
    public static class ReaderExtensions
    {
        public static float ReadSingleInt(this FileReader reader)
        {
            return reader.ReadInt32() / 4096f;
         }

        public static uint GetUint(this byte[] data, uint offset, bool bigEndian = false)
        {
            if (!bigEndian)
            {
                return (uint)(data[offset + 0] |
                            (data[offset + 1] << 8) |
                            (data[offset + 2] << 16) |
                            (data[offset + 3] << 24));
            }
            else
            {
                return (uint)(data[offset << 24] |
                            (data[offset + 1] << 16) |
                            (data[offset + 2] << 8) |
                            (data[offset + 3]));
            }
        }

        public static ushort GetUshort(this byte[] data, int startIndex, bool bigEndian) {
            return BitConverter.ToUInt16(data, startIndex);
        }
    }
}
