using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;
using MPLibrary.DS;

namespace MPLibrary.DS
{
    public class PaletteDataBlock : IBlockSection
    {
        public string Name { get; set; }
        public uint CompressionFlags { get; set; }
        public byte[] Data { get; set; } = new byte[0];

        public void Read(HsdfFile header, FileReader reader)
        {
            Name = ((NameBlock)header.ReadBlock(reader)).Name;
            uint size = reader.ReadUInt32();
            CompressionFlags = reader.ReadUInt32();
            Data = reader.ReadBytes((int)size);
            if (CompressionFlags == 1)
                Data = LZ77.Decompress(Data);
        }

        public void Write(HsdfFile header, FileWriter writer)
        {

        }
    }
}
