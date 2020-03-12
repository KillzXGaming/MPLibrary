using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Library.IO;
using System.Runtime.InteropServices;

namespace MPLibrary
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AttributeData
    {
        public uint NameOffset;
        public int TexAnimOffset;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] Unks;

        public float NbtEnable;
        public float TextureEnable;
        public float DontEdit;
        public AttrTransform TexAnimStart;
        public AttrTransform TexAnimEnd;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
        public byte[] Unks2;

        public int WrapS;
        public int WrapT;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] Unks3;

        public int MipmapMaxLOD;
        public int TextureFlags;
        public int TextureIndex;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AttrTransform
    {
        public Vector2XYZ Scale;
        public Vector2XYZ Position;
    }

    public class AttributeSection : HSFSection
    {
        public List<AttributeData> Attributes = new List<AttributeData>();
        public List<string> AttributeNames = new List<string>();

        public override void Read(FileReader reader, HsfFile header) {
            Attributes = reader.ReadMultipleStructs<AttributeData>(this.Count);

            for (int i = 0; i < Attributes.Count; i++)
                AttributeNames.Add(header.GetString(reader, Attributes[i].NameOffset));
        }

        public override void Write(FileWriter writer, HsfFile header) {
            for (int i = 0; i < Attributes.Count; i++) {
                var mat = Attributes[i];
                mat.NameOffset = (uint)header.GetStringOffset(AttributeNames[i]);
                writer.WriteStruct(mat);
            }
            writer.Align(4);
        }
    }

}
