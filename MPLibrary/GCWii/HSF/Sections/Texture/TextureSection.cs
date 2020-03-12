using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Library.IO;
using System.Runtime.InteropServices;
using Toolbox.Library;

namespace MPLibrary
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TextureInfo
    {
        public uint NameOffset;
        public uint MaxLOD;
        public byte Format;
        public byte Bpp; //to determine pallete types CI4/CI8
        public ushort Width;
        public ushort Height;
        public ushort PaletteEntries;
        public uint TextureTint;
        public int PaletteIndex;
        public uint Padding;// usually 0
        public uint DataOffset;
    }

    public class TextureSection : HSFSection
    {
        public List<TextureInfo> Textures = new List<TextureInfo>();
        public List<byte[]> ImageData = new List<byte[]>();
        public List<string> TextureNames = new List<string>();

        public override void Read(FileReader reader, HsfFile header) {
            Textures = reader.ReadMultipleStructs<TextureInfo>(this.Count);
            long pos = reader.Position;
            for (int i = 0; i < Textures.Count; i++)
            {
                TextureNames.Add(header.GetString(reader, Textures[i].NameOffset));

                reader.SeekBegin(pos + Textures[i].DataOffset);
                var format = FormatList[Textures[i].Format];
                if (format == Decode_Gamecube.TextureFormats.C8)
                {
                    if (Textures[i].Bpp == 4)
                        format = Decode_Gamecube.TextureFormats.C4;
                }
                var size = Decode_Gamecube.GetDataSize(format, Textures[i].Width, Textures[i].Height, false);
                var data = reader.ReadBytes(size);
                ImageData.Add(data);
            }
        }

        public override void Write(FileWriter writer, HsfFile header) {
            long texpos = writer.Position;
            for (int i = 0; i < Textures.Count; i++)
            {
                var tex = Textures[i];
                tex.NameOffset = (uint)header.GetStringOffset(TextureNames[i]);
                writer.WriteStruct(tex);
            }

            long datapos = writer.Position;
            for (int i = 0; i < Textures.Count; i++)
            {
                writer.Align(0x20);
                writer.WriteUint32Offset(texpos + 28 + (i * 32), datapos);
                writer.Write(ImageData[i]);
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
