using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;
using OpenTK;

namespace MPLibrary.MP10
{
    public class BNFM_Bone
    {
        public string Name { get; set; }
        public string Parent { get; set; }

        public uint NameHash { get; set; }
        public uint ParentNameHash { get; set; }

        public int ParentIndex { get; set; }

        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; }

        public Matrix4 Transform { get; set; }
        public Matrix4 Transform2 { get; set; }

        public int Index { get; set; }

        public Matrix4 GetTransform(List<BNFM_Bone> bones)
        {
            if (ParentIndex != -1)
                return Transform = Transform * bones[ParentIndex].GetTransform(bones);
            else
                return Transform;
        }

        public BNFM_Bone(BnfmFile header, FileReader reader)
        {
            Name = header.GetString(reader, reader.ReadUInt32());
            NameHash = reader.ReadUInt32();
            Parent = header.GetString(reader, reader.ReadUInt32());
            ParentNameHash = reader.ReadUInt32();
            uint boneOffset = reader.ReadUInt32();
            uint parentOffset = reader.ReadUInt32();
            uint unknown1 = reader.ReadUInt32();
            Index = reader.ReadInt32();
            uint unknown2 = reader.ReadUInt32();
            Position = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle());
            Scale = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle());
            reader.Seek(0x14);
        /*    float unknown3 = reader.ReadSingle();
            uint padding1 = reader.ReadUInt32();
            uint padding2 = reader.ReadUInt32();
            uint unknown4 = reader.ReadUInt32();
            uint padding3 = reader.ReadUInt32();*/
            Transform = reader.ReadMatrix4(true);
            Transform2 = reader.ReadMatrix4();
            uint unknownOffset = reader.ReadUInt32();
            uint unknownOffset2 = reader.ReadUInt32();
            uint unknown5 = reader.ReadUInt32();
        }
    }
}
