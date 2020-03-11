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
    public struct MaterialObject
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

    public class MaterialSection : HSFSection
    {
        public List<MaterialObject> Materials = new List<MaterialObject>();
        public List<string> MaterialNames = new List<string>();

        public override void Read(FileReader reader, HsfFile header) {
            Materials = reader.ReadMultipleStructs<MaterialObject>(this.Count);

            for (int i = 0; i < Materials.Count; i++)
                MaterialNames.Add(header.GetString(reader, Materials[i].NameOffset));
        }

        public override void Write(FileWriter writer, HsfFile header) {
            for (int i = 0; i < Materials.Count; i++) {
                var mat = Materials[i];
                mat.NameOffset = (uint)header.GetStringOffset(MaterialNames[i]);
                writer.WriteStruct(mat);
            }
        }
    }
}
