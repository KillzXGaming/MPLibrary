using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;

namespace MPLibrary.DS
{
    public class TextureMapperBlock : IBlockSection
    {
        public string Name { get; set; } = "";
        public string PaletteName { get; set; } = "";

        public void Read(HsdfFile header, FileReader reader)
        {
            reader.Seek(-4);
            uint size = reader.ReadUInt32();
            long pos = reader.Position;
            if (size > 12)
            {
                reader.Seek(8); //padding
                Name = ((NameBlock)header.ReadBlock(reader)).Name;
                if (reader.Position < pos + size)
                    PaletteName = ((NameBlock)header.ReadBlock(reader)).Name;
            }
        }

        public void Write(HsdfFile header, FileWriter writer)
        {

        }
    }
}
