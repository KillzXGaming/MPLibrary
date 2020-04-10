using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;

namespace MPLibrary.DS
{
    public class ObjectBlock : IBlockSection
    {
        public ObjectType Type { get; set; }

        public short ParentIndex { get; set; }

        public uint Unknown2 { get; set; }

        public string Name { get; set; }

        public float TranslateX;
        public float TranslateY;
        public float TranslateZ;
        public int RotateX;
        public int RotateY;
        public int RotateZ;
        public float ScaleX;
        public float ScaleY;
        public float ScaleZ;

        internal uint NameOffset;

        public MeshBlock MeshData { get; set; }

        public void Read(HsdfFile header, FileReader reader)
        {
            Type = (ObjectType)reader.ReadUInt16();
            ParentIndex = reader.ReadInt16();
            Unknown2 = reader.ReadUInt32();
            NameOffset = reader.ReadUInt32();
            TranslateX = reader.ReadSingleInt();
            TranslateY = reader.ReadSingleInt();
            TranslateZ = reader.ReadSingleInt();
            RotateX = reader.ReadInt32();
            RotateY = reader.ReadInt32();
            RotateZ = reader.ReadInt32();
            ScaleX = reader.ReadSingleInt();
            ScaleY = reader.ReadSingleInt();
            ScaleZ = reader.ReadSingleInt();

            //Read the next block
            if (Type == ObjectType.Mesh)
            {
                MeshData = (MeshBlock)header.ReadBlock(reader);
            }
        }

        public void Write(HsdfFile header, FileWriter writer)
        {

        }

        public enum ObjectType
        {
            Root = 0,
            Mesh = 2,
        }
    }
}
