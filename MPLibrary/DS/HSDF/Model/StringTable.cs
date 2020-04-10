using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;

namespace MPLibrary.DS
{
    public class StringTable : IBlockSection
    {
        public Dictionary<uint, string> Strings = new Dictionary<uint, string>();

        public void Read(HsdfFile header, FileReader reader)
        {
            reader.Seek(-4);
            uint size = reader.ReadUInt32();
            long pos = reader.Position;

            while (reader.Position < pos + size) {
                uint offset = (uint)(reader.Position - pos);
                Strings.Add(offset, reader.ReadZeroTerminatedString());
            }
            reader.Align(4);
        }

        public void Write(HsdfFile header, FileWriter writer)
        {

        }
    }
}
