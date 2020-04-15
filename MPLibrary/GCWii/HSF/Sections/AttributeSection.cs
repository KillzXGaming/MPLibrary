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
    public class AttributeData
    {
        public uint NameOffset;
        public int TexAnimOffset; //Replaced with Pointer to Texture Animation at Runtime
        public ushort Unknown1;
        public byte BlendingFlag; //Some king of blending flag. 2 is default. Often changed for multi textures
        public byte AlphaFlag; //Alpha textures use 1 else 0
        public float Unknown11;
        public int Unknown4;
        public float NbtEnable; //1.0 for enabled, 0.0 for disabled
        public float Unknown3;
        public float Unknown12;
        public float TextureEnable; //1.0 for enabled, 0.0 for disabled
        public float DontEdit;
        public AttrTransform TexAnimStart;
        public AttrTransform TexAnimEnd;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] Unks;

        public float Unknown5;
        public float Unknown6;
        public float Unknown7;

        public int WrapS;
        public int WrapT;

        public int Unknown8;
        public int Unknown9;
        public int Unknown10;

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

    public class AttributeAnimController
    {
        public float TranslateX { get; set; }
        public float TranslateY { get; set; }
        public float TranslateZ { get; set; }

        public float RotateX { get; set; }
        public float RotateY { get; set; }
        public float RotateZ { get; set; }

        public float ScaleX { get; set; }
        public float ScaleY { get; set; }
        public float ScaleZ { get; set; }
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
