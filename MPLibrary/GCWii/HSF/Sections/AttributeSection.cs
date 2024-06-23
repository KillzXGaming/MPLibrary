using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using System.Runtime.InteropServices;

namespace MPLibrary.GCN
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class AttributeData
    {
        public uint NameOffset;
        public int TexAnimOffset; //Replaced with Pointer to Texture Animation at Runtime
        public ushort Unknown1;
        public CombinerBlend BlendingFlag = CombinerBlend.Additive;
        public byte AlphaFlag; //Alpha textures use 1 else 0
        public float BlendTextureAlpha = 1.0f; //Blend with texture alpha else use register color 2 from alpha output
        public int Unknown2 = 1;
        public float NbtEnable = 0.0f; //1.0 for enabled, 0.0 for disabled.
        public float Unknown3 = -1f;
        public float Unknown4;
        public float TextureEnable = 1.0f; //1.0 for enabled, 0.0 for disabled
        public float Unknown11;
        public AttrTransform TexAnimStart;
        public AttrTransform TexAnimEnd;

        public float Unknown13;

        public Vector3XYZ Rotation = new Vector3XYZ();

        public float Unknown5 = 1.0f;
        public float Unknown6 = 1.0f;
        public float Unknown7 = 1.0f;

        public WrapMode WrapS = WrapMode.Repeat;
        public WrapMode WrapT = WrapMode.Repeat;

        public int Unknown8 = 1;
        public int Unknown9 = 79;
        public int Unknown10 = 0;

        public int MipmapMaxLOD = 1;
        public int TextureFlags;
        public int TextureIndex;

        public AttributeData()
        {
            TexAnimStart = new AttrTransform()
            {
                Scale = new Vector2XYZ(1, 1),
            };
            TexAnimEnd = new AttrTransform()
            {
                Scale = new Vector2XYZ(1, 1),
            };
        }
    }

    public enum CombinerBlend : byte
    {
        /// <summary>
        /// Mixes current and last stages by texture alpha with a new stage
        /// </summary>
        TransparencyMix = 0,
        /// <summary>
        /// Combines current and last stage by adding.
        /// </summary>
        Additive = 2,
    }

    public enum WrapMode
    {
        Clamp,
        Repeat,
        Mirror,
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

        public int TextureIndex { get; set; } = -1;

        public float CombinerBlending { get; set; } = 1.0f;
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
