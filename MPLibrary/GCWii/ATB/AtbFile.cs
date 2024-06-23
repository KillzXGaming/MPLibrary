using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using Toolbox.Core;
using System.Runtime.InteropServices;
using System.IO;

namespace MPLibrary.GCN
{
    public class AtbFile
    {
        public List<AtbTextureInfo> Textures = new List<AtbTextureInfo>();
        public List<BankData> Banks = new List<BankData>();
        public List<PatternData> Patterns = new List<PatternData>();

        public ushort NumReferences { get; set; }

        public int FileIndex = 0;

        public AtbFile() { }

        public AtbFile(Stream stream)
        {
            using (var reader = new FileReader(stream)) {
                Read(reader);
            }
        }

        public AtbFile(string fileName) {
            using (var reader = new FileReader(fileName)) {
                Read(reader);
            }
        }

        public void Save(string fileName)
        {
            using (var writer = new FileWriter(fileName)) {
                Write(writer);
            }
        }

        public void Save(Stream stream)
        {
            using (var writer = new FileWriter(stream)) {
                Write(writer);
            }
        }

        void Read(FileReader reader)
        {
            reader.SetByteOrder(true);
            ushort numBanks = reader.ReadUInt16();
            ushort numPatterns = reader.ReadUInt16();
            ushort numTextures = reader.ReadUInt16();
            NumReferences = reader.ReadUInt16();
            uint bankOffset = reader.ReadUInt32();
            uint patternDataOffset = reader.ReadUInt32();
            uint textureDataOffset = reader.ReadUInt32();

            for (int i = 0; i < numBanks; i++)
            {
                reader.SeekBegin(bankOffset + (i * 0x8));
                Banks.Add(new BankData(reader));
            }

            for (int i = 0; i < numPatterns; i++)
            {
                reader.SeekBegin(patternDataOffset + (i * 0x10));
                Patterns.Add(new PatternData(reader));
            }

            for (int i = 0; i < numTextures; i++)
            {
                reader.SeekBegin(textureDataOffset + (i * 0x14));

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

        void Write(FileWriter writer)
        {
            writer.SetByteOrder(true);
            writer.Write((ushort)Banks.Count);
            writer.Write((ushort)Patterns.Count);
            writer.Write((ushort)Textures.Count);
            writer.Write(NumReferences);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);

            if (Patterns.Count > 0) {
                long patternsPos = writer.Position;

                writer.WriteUint32Offset(0xC);
                for (int i = 0; i < Patterns.Count; i++)
                    Patterns[i].Write(writer);

                for (int i = 0; i < Patterns.Count; i++)
                {
                    if (Patterns[i].Layers.Count > 0)
                    {
                        writer.WriteUint32Offset(patternsPos + 0xC + (i * 0x10));
                        foreach (var layer in Patterns[i].Layers)
                            writer.WriteStruct(layer);
                    }
                }
            }
            if (Banks.Count > 0) {
                long banksPos = writer.Position;

                writer.WriteUint32Offset(0x8);
                for (int i = 0; i < Banks.Count; i++)
                    Banks[i].Write(writer);

                for (int i = 0; i < Banks.Count; i++)
                {
                    if (Banks[i].AnimFrames.Count > 0)
                    {
                        writer.WriteUint32Offset(banksPos + 4 + (i * 0x08));
                        foreach (var frame in Banks[i].AnimFrames)
                            writer.WriteStruct(frame);
                    }
                }
            }
            if (Textures.Count > 0) {
                writer.WriteUint32Offset(0x10);
                long texPos = writer.Position;
                for (int i = 0; i < Textures.Count; i++)
                {
                    writer.Write(Textures[i].Bpp);
                    writer.Write(Textures[i].Format);
                    if (Textures[i].PaletteData != null)
                        writer.Write((ushort)(Textures[i].PaletteData.Length / 2));
                    else
                        writer.Write((ushort)0);
                    writer.Write(Textures[i].Width);
                    writer.Write(Textures[i].Height);
                    writer.Write(Textures[i].ImageData.Length);
                    writer.Write(0);
                    writer.Write(0);
                }

                writer.AlignBytes(32, 0x88);
                for (int i = 0; i < Textures.Count; i++)
                {
                    writer.WriteUint32Offset(texPos + 0x0C + (0x14 * i));
                    if (Textures[i].PaletteData != null)
                        writer.Write(Textures[i].PaletteData);

                    writer.WriteUint32Offset(texPos + 0x10 + (0x14 * i));
                    writer.Write(Textures[i].ImageData);
                }
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
        public byte[] PaletteData = new byte[0];
    }

    public class BankData
    {
        public List<AnimFrame> AnimFrames = new List<AnimFrame>();

        public BankData(FileReader reader)
        {
            short frameCount = reader.ReadInt16();
            reader.ReadInt16(); //padding
            uint frameArrayOffset = reader.ReadUInt32();

            if (frameCount > 0) {
                reader.SeekBegin(frameArrayOffset);
                AnimFrames = reader.ReadMultipleStructs<AnimFrame>(frameCount);
            }
        }

        public void Write(FileWriter writer)
        {
            writer.Write((ushort)AnimFrames.Count);
            writer.Write((ushort)0);
            writer.Write(0);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class AnimFrame
    {
        public short PatternIndex;  
        public short FrameLength;
        public short ShiftX;
        public short ShiftY;
        public short Flip;
        public short Unk;
    }

    public class PatternData
    {
        public List<LayerData> Layers = new List<LayerData>();

        public short CenterX;
        public short CenterY;
        public short Width;
        public short Height;
        public short Padding;

        public PatternData(FileReader reader)
        {
            short numLayers = reader.ReadInt16();
            CenterX = reader.ReadInt16();
            CenterY = reader.ReadInt16();
            Width = reader.ReadInt16();
            Height = reader.ReadInt16();
            reader.ReadInt16(); //PADDING
            uint layerArrayOffset = reader.ReadUInt32();

            if (numLayers > 0)
            {
                reader.SeekBegin(layerArrayOffset);
                Layers = reader.ReadMultipleStructs<LayerData>(numLayers);
            }
        }

        public void Write(FileWriter writer)
        {
            writer.Write((ushort)Layers.Count);
            writer.Write(CenterX);
            writer.Write(CenterY);
            writer.Write(Width);
            writer.Write(Height);
            writer.Write((ushort)0);
            writer.Write(0);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class LayerData
    {
        public byte Alpha;
        public byte Flip;
        public short TextureIndex;
        public short TexCoordTopLeftX;
        public short TexCoordTopLeftY;
        public short TexCoordWidth;
        public short TexCoordHeight;
        public short ShiftX;
        public short ShiftY;
        public short VertexTopLeftX;
        public short VertexTopLeftY;
        public short VertexTopRightX;
        public short VertexTopRightY;
        public short VertexBottomRightX;
        public short VertexBottomRightY;
        public short VertexBottomLeftX;
        public short VertexBottomLeftY;
    }
}
