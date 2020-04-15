using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;
using Toolbox.Library;
using System.Runtime.InteropServices;
using System.IO;

namespace MPLibrary.GCN
{
    public class AtbFile
    {
        public List<AtbTextureInfo> Textures = new List<AtbTextureInfo>();

        public AtbFile() { }

        public AtbFile(Stream stream)
        {
            using (var reader = new FileReader(stream)) {
                Read(reader);
            }
        }

        public void Read(FileReader reader)
        {
            reader.SetByteOrder(true);
            ushort numBanks = reader.ReadUInt16();
            ushort numPatterns = reader.ReadUInt16();
            ushort numTextures = reader.ReadUInt16();
            ushort numReferences = reader.ReadUInt16();
            uint bankOffset = reader.ReadUInt32();
            uint patternDataOffset = reader.ReadUInt32();
            uint textureDataOffset = reader.ReadUInt32();

            reader.SeekBegin(textureDataOffset);
            for (int i = 0; i < numTextures; i++)
            {
                AtbTextureInfo tex = new AtbTextureInfo();
                Textures.Add(tex);

                tex.Bpp = reader.ReadByte();
                tex.Format = reader.ReadByte();
                ushort PaletteSize = reader.ReadUInt16();
                tex.Width = reader.ReadUInt16();
                tex.Height = reader.ReadUInt16();
                uint ImageSize = reader.ReadUInt32();
                uint PaletteOffset = reader.ReadUInt32();
                uint ImageOffset = reader.ReadUInt32();

                if (PaletteSize != 0)
                    tex.PaletteData = reader.getSection(PaletteOffset, (uint)(PaletteSize * 2));
                if (ImageSize != 0)
                    tex.ImageData = reader.getSection(ImageOffset, ImageSize);
            }
        }
    }

    public class AtbTextureInfo
    {
        public byte Bpp;
        public byte Format;
        public ushort Width;
        public ushort Height;

        public byte[] ImageData;
        public byte[] PaletteData;
    }

    public class AtbBankData
    {
        public short FrameCount;
        public short padding;
        public int animFrameArrayOffset;
    }
}
