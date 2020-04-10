using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;

namespace MPLibrary.DS
{
    public class AttributeBlock
    {
        public string Name { get; set; }
        public string TextureName { get; set; }

        public uint Flags { get; set; }
        public float TranslateX;
        public float TranslateY;
        public float TranslateZ;
        public int RotateX;
        public int RotateY;
        public int RotateZ;
        public float ScaleX;
        public float ScaleY;
        public float ScaleZ;
        public ushort Unknown8;
        public ushort Unknown9;

        internal uint NameOffset;
        internal ushort TextureNameOffset;

        public AttributeBlock(FileReader reader)
        {
            NameOffset = reader.ReadUInt32();
            Flags = reader.ReadUInt32();
            TranslateX = reader.ReadSingleInt();
            TranslateY = reader.ReadSingleInt();
            TranslateZ = reader.ReadSingleInt();
            RotateX = reader.ReadInt32();
            RotateY = reader.ReadInt32();
            RotateZ = reader.ReadInt32();
            ScaleX = reader.ReadSingleInt();
            ScaleY = reader.ReadSingleInt();
            ScaleZ = reader.ReadSingleInt();
            Unknown8 = reader.ReadUInt16();
            TextureNameOffset = reader.ReadUInt16();
        }
    }
}
