using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Library.IO;
using OpenTK;

namespace MPLibrary
{
    public class NormalSection : HSFSection
    {
        public DataType TypeFlag;

        public enum DataType
        {
            Float,
            Sbyte,
        }

        public override void Read(FileReader reader, HsfFile header)
        {
            List<ComponentData> Components = reader.ReadMultipleStructs<ComponentData>(this.Count);
            long startingOffset = reader.Position;
            TypeFlag = DataType.Float;

            if (Components.Count >= 2) {
                var pos = startingOffset + Components[0].DataOffset + Components[0].DataCount * 3;
                if (pos % 0x20 != 0)
                    pos += 0x20 - (pos % 0x20);
                if (Components[1].DataOffset == pos - startingOffset)
                    TypeFlag = DataType.Sbyte;
            }
            else
                TypeFlag = DataType.Sbyte;

            foreach (var comp in Components) {
                reader.SeekBegin(startingOffset + comp.DataOffset);

                var normals = new List<Vector3>();
                for (int i = 0; i < comp.DataCount; i++)
                {
                    if (TypeFlag == DataType.Sbyte)
                        normals.Add(new Vector3(reader.ReadSByte(), reader.ReadSByte(), reader.ReadSByte()));
                    else
                        normals.Add(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
                }
                header.AddNormalComponent(reader, comp, normals);
            }
        }

        public override void Write(FileWriter writer, HsfFile header)
        {
            var meshes = header.GetAllMeshes().Where(x => x.Positions.Count > 0).ToList();

            long posStart = writer.Position;
            foreach (var mesh in meshes)
            {
                writer.Write(header.GetStringOffset(mesh.Name));
                writer.Write(mesh.Normals.Count);
                writer.Write(uint.MaxValue);
            }

            long dataPos = writer.Position;
            for (int i = 0; i < meshes.Count; i++)
            {
                writer.Align(0x20);
                writer.WriteUint32Offset(posStart + 8 + (i * 12), dataPos);
                for (int j = 0; j < meshes[i].Normals.Count; j++)
                {
                    if (TypeFlag == DataType.Sbyte)
                    {
                        writer.Write((sbyte)(meshes[i].Normals[j].X));
                        writer.Write((sbyte)(meshes[i].Normals[j].Y));
                        writer.Write((sbyte)(meshes[i].Normals[j].Z));
                    }
                    else
                        writer.Write(meshes[i].Normals[j]);
                }
            }
        }
    }

}
