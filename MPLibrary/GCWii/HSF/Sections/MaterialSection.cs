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
    public class MaterialObject
    {
        public uint NameOffset; //0
        public int Unknown; //0x4
        public ushort AltFlags; //0x8
        public LightingChannelFlags VertexMode; //0xA
        public ColorRGB_8 AmbientColor; //0xB
        public ColorRGB_8 MaterialColor; //0x1E
        public ColorRGB_8 ShadowColor; //0x11
        public float HiliteScale; //0x14
        public float Unknown2; //0x18
        public float TransparencyInverted; //0x1C
        public float Unknown3; //0x20
        public float Unknown4; //0x24
        public float ReflectionIntensity; //0x28
        public float Unknown5 = 1f; //0x2C
        public int MaterialFlags; //0x30
        public int TextureCount; //0x34
        public int FirstSymbol; //0x38
    }

    public enum LightingChannelFlags : byte
    {
        NoLighting = 0, //Flat shading
        Lighting = 1, //Lighting used
        LightingSpecular = 2, //Second light channel used for specular
        LightingSpecular2 = 3, //Same output as LightingSpecular. Not sure if used.
        VertexAlphaOnly = 4, //Vertex colors but only with alpha
        VertexColorsWithAlpha = 5, //Vertex colors + alpha
    }

    public class MatAnimController
    {
        public float LitAmbientColorR;
        public float LitAmbientColorG;
        public float LitAmbientColorB;

        public float AmbientColorR;
        public float AmbientColorG;
        public float AmbientColorB;

        public float ShadowColorR;
        public float ShadowColorG;
        public float ShadowColorB;

        public float HiliteScale;
        public float TransparencyInverted;
        public float ReflectionIntensity;
        public float Unknown3;
        public float Unknown4;
        public float Unknown5;
    }

    public class MaterialSection : HSFSection
    {
        public override void Read(FileReader reader, HsfFile header) {
            List<MaterialObject> Materials = reader.ReadMultipleStructs<MaterialObject>(this.Count);
            for (int i = 0; i < Materials.Count; i++)
            {
                string name = header.GetString(reader, Materials[i].NameOffset);
                header.AddMaterial(Materials[i],name);
            }
        }

        public override void Write(FileWriter writer, HsfFile header) {
            for (int i = 0; i < header.Materials.Count; i++) {
                var mat = header.Materials[i];
                mat.MaterialData.NameOffset = (uint)header.GetStringOffset(mat.Name);
                writer.WriteStruct(mat.MaterialData);
            }
            writer.Align(4);
        }
    }
}
