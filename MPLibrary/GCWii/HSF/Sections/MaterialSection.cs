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
        public uint NameOffset;
        public int Unknown;
        public ushort AltFlags;
        public byte VertexMode;
        public ColorRGB_8 LitAmbientColor;
        public ColorRGB_8 AmbientColor;
        public ColorRGB_8 ShadowColor;
        public float HiliteScale;
        public float Unknown2;
        public float TransparencyInverted;
        public float Unknown3;
        public float Unknown4;
        public float ReflectionIntensity;
        public float Unknown5;
        public int MaterialFlags;
        public int TextureCount;
        public int FirstSymbol;
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
