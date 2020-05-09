using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Toolbox.Core;
using MPLibrary.GCN;
using Newtonsoft.Json;

namespace MPLibrary.GCN
{
    public class HSFMaterialWrapper : STGenericMaterial
    {
        public HSF ParentHSF;

        public Material Material { get; set; }
        public List<AttributeData> Attributes { get; set; } = new List<AttributeData>();
        public Mesh Mesh { get; set; }

        public HSFMaterialWrapper(HSF hsf)
        {
            ParentHSF = hsf;

        }

        public void FromString(string text)
        {
            Material.MaterialData = JsonConvert.DeserializeObject<MaterialObject>(text);
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(Material.MaterialData, Formatting.Indented);
        }

        public class MaterialData
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
    }
}
