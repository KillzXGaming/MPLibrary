using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;
using System.Runtime.InteropServices;

namespace MPLibrary.DS
{
    public class ModelBlock : IBlockSection
    {
        public List<MaterialData> Materials = new List<MaterialData>();
        public List<AttributeBlock> Attributes = new List<AttributeBlock>();
        public List<MatrixBlock> Matrices = new List<MatrixBlock>();
        public List<ObjectBlock> Objects = new List<ObjectBlock>();

        public void Read(HsdfFile header, FileReader reader)
        {
            uint unknown1 = reader.ReadUInt32();
            uint unknown2 = reader.ReadUInt32();
            uint unknown3 = reader.ReadUInt32();
            uint unknown4 = reader.ReadUInt32();
            uint unknown5 = reader.ReadUInt32();
            uint unknown6 = reader.ReadUInt32();
            uint unknown7 = reader.ReadUInt32();
            uint unknown8 = reader.ReadUInt32();
            uint unknown9 = reader.ReadUInt32();
            uint unknown10 = reader.ReadUInt32();
            ushort numObjects = reader.ReadUInt16();
            ushort numMaterials = reader.ReadUInt16();
            ushort numTextures = reader.ReadUInt16();
            ushort numMatrices = reader.ReadUInt16();

            var materials = reader.ReadMultipleStructs<MaterialBlock>(numMaterials);
            for (int i = 0; i < numTextures; i++)
                Attributes.Add(new AttributeBlock(reader));
            for (int i = 0; i < numMatrices; i++)
                Matrices.Add(new MatrixBlock(reader));
            for (int i = 0; i < numObjects; i++)
                Objects.Add((ObjectBlock)header.ReadBlock(reader));

            StringTable table = (StringTable)header.ReadBlock(reader);
            for (int i = 0; i < numObjects; i++)
            {
                if (table.Strings.ContainsKey(Objects[i].NameOffset))
                    Objects[i].Name = table.Strings[Objects[i].NameOffset];
            }

            for (int i = 0; i < numMaterials; i++)
            {
                var mat = new MaterialData(materials[i]);
                if (table.Strings.ContainsKey(materials[i].NameOffset))
                    mat.Name = table.Strings[materials[i].NameOffset];

                Materials.Add(mat);
            }

            for (int i = 0; i < numTextures; i++)
            {
                if (table.Strings.ContainsKey(Attributes[i].NameOffset))
                {
                    Attributes[i].Name = table.Strings[Attributes[i].NameOffset];
                    Attributes[i].TextureName = table.Strings[Attributes[i].TextureNameOffset];
                }
            }
        }

        public void Write(HsdfFile header, FileWriter writer)
        {

        }
    }
}
