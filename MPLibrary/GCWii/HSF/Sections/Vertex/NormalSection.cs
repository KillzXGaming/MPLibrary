using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using System.Numerics;
using static Toolbox.Core.DDS;

namespace MPLibrary.GCN
{
    public class NormalSection : HSFSection
    {
        public List<ComponentData> Components = new List<ComponentData>();

        private long startingOffset;

        public override void Read(FileReader reader, HsfFile header)
        {
            Components = reader.ReadMultipleStructs<ComponentData>(this.Count);
            startingOffset = reader.Position;

            foreach (var node in header.ObjectData.Objects)
            {
                var data = node.Data;

                if (data.NormalIndex <= -1 || data.NormalIndex > Components.Count)
                    continue;

                var comp = Components[data.NormalIndex];
                reader.SeekBegin(startingOffset + comp.DataOffset);

                var normals = new List<Vector3>();
                for (int i = 0; i < comp.DataCount; i++)
                {
                    if (data.CenvCount == 0)
                        normals.Add(new Vector3(
                            reader.ReadSByte() / (float)sbyte.MaxValue,
                            reader.ReadSByte() / (float)sbyte.MaxValue,
                            reader.ReadSByte() / (float)sbyte.MaxValue));
                    else
                        normals.Add(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
                }
                header.AddNormalComponent(Components.IndexOf(comp), normals);
            }
        }

        public void GetComponentData(FileReader reader, int index, bool is_sbyte)
        {
            var comp = Components[index];
            reader.SeekBegin(startingOffset + comp.DataOffset);

            var normals = new List<Vector3>();
            for (int i = 0; i < comp.DataCount; i++)
            {
                if (is_sbyte)
                    normals.Add(new Vector3(
                        reader.ReadSByte() / (float)sbyte.MaxValue,
                        reader.ReadSByte() / (float)sbyte.MaxValue,
                        reader.ReadSByte() / (float)sbyte.MaxValue));
                else
                    normals.Add(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            }
        }

        public override void Write(FileWriter writer, HsfFile header)
        {
            var meshes = header.Meshes.Where(x => x.Positions.Count > 0).ToList();

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
                    if (!meshes[i].HasEnvelopes)
                    {
                        writer.Write((sbyte)(meshes[i].Normals[j].X * sbyte.MaxValue));
                        writer.Write((sbyte)(meshes[i].Normals[j].Y * sbyte.MaxValue));
                        writer.Write((sbyte)(meshes[i].Normals[j].Z * sbyte.MaxValue));
                    }
                    else
                    {
                        writer.Write(meshes[i].Normals[j].X);
                        writer.Write(meshes[i].Normals[j].Y);
                        writer.Write(meshes[i].Normals[j].Z);
                    }
                }
            }
            writer.Align(4);
        }
    }

}
