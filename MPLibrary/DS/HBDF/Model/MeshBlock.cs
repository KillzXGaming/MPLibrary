using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using System.Runtime.InteropServices;

namespace MPLibrary.DS
{
    public class MeshBlock : IBlockSection
    {
        public float ScaleX;
        public float ScaleY;
        public float ScaleZ;

        public uint Unknown1;
        public uint Unknown2;
        public uint Unknown3;
        public uint Unknown4;

        public List<PolyGroup> PolyGroups = new List<PolyGroup>();

        public byte[] Data;

        public void Read(HsdfFile header, FileReader reader)
        {
            ScaleX = reader.ReadSingleInt();
            ScaleY = reader.ReadSingleInt();
            ScaleZ = reader.ReadSingleInt();
            Unknown1 = reader.ReadUInt32();
            Unknown2 = reader.ReadUInt32();
            Unknown3 = reader.ReadUInt32();
            Unknown4 = reader.ReadUInt32();
            ushort numBlocks = reader.ReadUInt16();
            ushort dataSize = reader.ReadUInt16();
            PolyGroups = reader.ReadMultipleStructs<PolyGroup>((int)numBlocks);
            Data = reader.ReadBytes(dataSize);
        }

        public void Write(HsdfFile header, FileWriter writer)
        {

        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PolyGroup
        {
            public ushort MaterialIndex;
            public ushort Unknown2;
            public ushort FaceStart;
            public ushort FaceCount;
        }
    }
}
