using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using System.Runtime.InteropServices;
using System.Numerics;

namespace MPLibrary.GCN
{
    public class ColorSection : HSFSection
    {
        public override void Read(FileReader reader, HsfFile header)
        {
            List<ComponentData> Components = reader.ReadMultipleStructs<ComponentData>(this.Count);
            long pos = reader.Position;
            foreach (var comp in Components) {
                reader.SeekBegin(pos + comp.DataOffset);

                List<Vector4> colors = new List<Vector4>();
                for (int i = 0; i < comp.DataCount; i++)
                    colors.Add(new Vector4(
                        reader.ReadByte() / 255f, reader.ReadByte() / 255f,
                        reader.ReadByte() / 255f, reader.ReadByte() / 255f));

                header.AddColorComponent(Components.IndexOf(comp), colors);
            }
        }

        public override void Write(FileWriter writer, HsfFile header)
        {
            long posStart = writer.Position;

            var meshes = header.Meshes.Where(x => x.Color0.Count > 0).ToList();
            foreach (var mesh in meshes)
            {
                writer.Write(header.GetStringOffset(mesh.Name));
                writer.Write(mesh.Color0.Count);
                writer.Write(uint.MaxValue);
            }

            long dataPos = writer.Position;
            for (int i = 0; i < meshes.Count; i++)
            {
                meshes[i].ObjectData.ColorIndex = i;

                writer.Align(0x20);
                writer.WriteUint32Offset(posStart + 8 + (i * 12), dataPos);
                for (int j = 0; j < meshes[i].Color0.Count; j++)
                {
                    writer.Write((byte)(meshes[i].Color0[j].X * 255));
                    writer.Write((byte)(meshes[i].Color0[j].Y * 255));
                    writer.Write((byte)(meshes[i].Color0[j].Z * 255));
                    writer.Write((byte)(meshes[i].Color0[j].W * 255));
                }
            }
            writer.Align(4);
        }
    }

}
