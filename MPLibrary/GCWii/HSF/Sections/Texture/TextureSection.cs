using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using System.Runtime.InteropServices;
using Toolbox.Core;

namespace MPLibrary.GCN
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TextureInfo
    {
        public uint NameOffset; //0x0
        public uint MaxLOD; //0x4
        public byte Format; //0x8
        public byte Bpp; //0x9 //to determine pallete types CI4/CI8
        public ushort Width; //0xA
        public ushort Height; //0xC
        public ushort PaletteEntries; //0xE
        public uint TextureTint; //0x10 Used for grayscale (I4, I8) types. Color blends with tev stages as color
        public int PaletteIndex; //0x14
        public uint Padding;//0x18 // usually 0
        public uint DataOffset; //0x1C
    }

    public class TextureSection : HSFSection
    {
        public override void Read(FileReader reader, HsfFile header) {
            var textures = reader.ReadMultipleStructs<TextureInfo>(this.Count);
            long pos = reader.Position;
            for (int i = 0; i < textures.Count; i++)
            {
                string name = header.GetString(reader, textures[i].NameOffset);

                reader.SeekBegin(pos + textures[i].DataOffset);
                var format = FormatList[textures[i].Format];
                if (format == Decode_Gamecube.TextureFormats.C8)
                {
                    if (textures[i].Bpp == 4)
                        format = Decode_Gamecube.TextureFormats.C4;
                }
                var size = Decode_Gamecube.GetDataSize(format, textures[i].Width, textures[i].Height, false);
                var data = reader.ReadBytes(size);
                header.AddTexture(name, textures[i], data);
            }
        }

        public override void Write(FileWriter writer, HsfFile header) {
            long texpos = writer.Position;
            for (int i = 0; i < header.Textures.Count; i++)
            {
                var tex = header.Textures[i].TextureInfo;
                tex.NameOffset = (uint)header.GetStringOffset(header.Textures[i].Name);
                writer.WriteStruct(tex);
            }

            long datapos = writer.Position;
            for (int i = 0; i < header.Textures.Count; i++)
            {
                writer.Align(0x20);
                writer.WriteUint32Offset(texpos + 28 + (i * 32), datapos);
                writer.Write(header.Textures[i].ImageData);
            }
            writer.Align(8);
        }

        public static Dictionary<int, Decode_Gamecube.TextureFormats> FormatList = new Dictionary<int, Decode_Gamecube.TextureFormats>()
        {
            { 0x00, Decode_Gamecube.TextureFormats.I8 },
            { 0x01, Decode_Gamecube.TextureFormats.I8 },
            { 0x02, Decode_Gamecube.TextureFormats.IA4 },
            { 0x03, Decode_Gamecube.TextureFormats.IA8 },
            { 0x04, Decode_Gamecube.TextureFormats.RGB565 },
            { 0x05, Decode_Gamecube.TextureFormats.RGB5A3 },
            { 0x06, Decode_Gamecube.TextureFormats.RGBA32 },
            { 0x07, Decode_Gamecube.TextureFormats.CMPR },
            { 0x09, Decode_Gamecube.TextureFormats.C8 }, //C4 if BPP == 4
            { 0x0A, Decode_Gamecube.TextureFormats.C8 }, //C4 if BPP == 4
            { 0x0B, Decode_Gamecube.TextureFormats.C8 }, //C4 if BPP == 4
        };
    }

}
