using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using System.Numerics;

namespace MPLibrary.GCN
{
    public class PositionSection : HSFSection
    {
        public List<ComponentData> Components = new List<ComponentData>();

        public override void Read(FileReader reader, HsfFile header)
        {
            Components = reader.ReadMultipleStructs<ComponentData>(this.Count);
            long pos = reader.Position;
            foreach (var comp in Components) {
                reader.SeekBegin(pos + comp.DataOffset);

                List<Vector3> positions = new List<Vector3>();
                for (int i = 0; i < comp.DataCount; i++)
                    positions.Add(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));

                header.AddPositionComponent(Components.IndexOf(comp), positions);
            }
        }

        public override void Write(FileWriter writer, HsfFile header)
        {
            long posStart = writer.Position;

            var meshes = header.Meshes.Where(x => x.Positions.Count > 0).ToList();
            foreach (var mesh in meshes)
            {
                writer.Write(header.GetStringOffset(mesh.Name));
                writer.Write(mesh.Positions.Count);
                writer.Write(uint.MaxValue);
            }

            long dataPos = writer.Position;
            for (int i = 0; i < meshes.Count; i++) {
                writer.Align(0x20);
                writer.WriteUint32Offset(posStart + 8 + (i * 12), dataPos);
                for (int j = 0; j < meshes[i].Positions.Count; j++)
                {
                    writer.Write(meshes[i].Positions[j].X);
                    writer.Write(meshes[i].Positions[j].Y);
                    writer.Write(meshes[i].Positions[j].Z);
                }
            }
            writer.Align(4);
        }
    }

}
