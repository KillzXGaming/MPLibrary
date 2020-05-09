using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using MPLibrary.DS;

namespace MPLibrary.DS
{
    public class ImageBlock : IBlockSection
    {
        public PaletteDataBlock PaletteData = new PaletteDataBlock();

        public string Name { get; set; }

        public NitroTex.NitroTexFormat Format { get; set; }
        public ushort Width { get; set; }
        public ushort Height { get; set; }
        public uint Params { get; set; }

        public uint CompressionFlags { get; set; }

        public byte[] ImageData { get; set; }
        public byte[] ImageData4x4 { get; set; }

        public bool TransparentColor
        {
            get { return ((Params >> 29) & 0x01) == 1; }
        }

        public void Read(HsdfFile header, FileReader reader)
        {
            Name = ((NameBlock)header.ReadBlock(reader)).Name;
            Format = (NitroTex.NitroTexFormat)reader.ReadUInt32();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            Params = reader.ReadUInt32();
            uint textureSize = reader.ReadUInt32();
            uint tex4x4Size = reader.ReadUInt32();
            CompressionFlags = reader.ReadUInt32();
            ImageData = reader.ReadBytes((int)textureSize);
            if (CompressionFlags == 1)
                ImageData = LZ77.Decompress(ImageData);
            ImageData4x4 = reader.ReadBytes((int)tex4x4Size);
        }

        public void Write(HsdfFile header, FileWriter writer)
        {

        }
    }
}
