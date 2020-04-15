using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;
using System.Runtime.InteropServices;

namespace MPLibrary.GCN
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PaletteInfo
    {
        public uint NameOffset;
        public int Format;
        public int NumPalette;
        public uint DataOffset;
    }

    public class PaletteSection : HSFSection
    {
        public List<PaletteInfo> Palettes = new List<PaletteInfo>();
        public List<byte[]> PaletteData = new List<byte[]>();

        public override void Read(FileReader reader, HsfFile header)
        {
            Palettes = reader.ReadMultipleStructs<PaletteInfo>(this.Count);
            long pos = reader.Position;
            for (int i = 0; i < Palettes.Count; i++)
            {
                reader.SeekBegin(pos + Palettes[i].DataOffset);
                PaletteData.Add(reader.ReadBytes(Palettes[i].NumPalette * 2));
            }

            header.AddPalette(Palettes, PaletteData);
        }

        public override void Write(FileWriter writer, HsfFile header)
        {
            long startPos = writer.Position;
            for (int i = 0; i < Palettes.Count; i++)
                writer.WriteStruct(Palettes[i]);

            long dataPos = writer.Position;
            for (int i = 0; i < Palettes.Count; i++)
            {
                writer.Align(0x20);
                writer.WriteUint32Offset(startPos + 12 + (i * 16), dataPos);
                writer.Write(PaletteData[i]);
            }
            writer.Align(4);
        }
    }

}
