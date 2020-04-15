using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;
using OpenTK;

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
        public float RotateX;
        public float RotateY;
        public float RotateZ;
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
            RotateX = reader.ReadInt32() / 16384f;
            RotateY = reader.ReadInt32() / 16384f;
            RotateZ = reader.ReadInt32() / 16384f;
            ScaleX = reader.ReadSingleInt();
            ScaleY = reader.ReadSingleInt();
            ScaleZ = reader.ReadSingleInt();

            //Read the next block
            if (Type == ObjectType.Mesh)
            {
                MeshData = (MeshBlock)header.ReadBlock(reader);
            }
        }

        public Matrix4 GetTransform(List<ObjectBlock> objects)
        {
            if (ParentIndex != -1)
                return GetTransform() * objects[ParentIndex].GetTransform(objects);
            else
                return GetTransform();
        }

        public Matrix4 GetTransform()
        {
            Matrix4 meshScale = Matrix4.Identity;
            if (MeshData != null)
                meshScale = Matrix4.CreateScale(MeshData.ScaleX, MeshData.ScaleY, MeshData.ScaleZ);

            return
                 Matrix4.CreateScale(ScaleX, ScaleY, ScaleZ) *
                 (Matrix4.CreateRotationX(RotateX) *
                Matrix4.CreateRotationY(RotateY) *
                Matrix4.CreateRotationZ(RotateZ)) *
                Matrix4.CreateTranslation(TranslateX, TranslateY, TranslateZ) *
                meshScale;  
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
