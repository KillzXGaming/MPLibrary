using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using System.Runtime.InteropServices;
using OpenTK;

namespace MPLibrary.GCN
{
    public class TexCoordSection : HSFSection
    {
        public override void Read(FileReader reader, HsfFile header)
        {
            List<ComponentData> Components = reader.ReadMultipleStructs<ComponentData>(this.Count);
            long pos = reader.Position;
            foreach (var comp in Components) {
                reader.SeekBegin(pos + comp.DataOffset);

                List<Vector2> uvs = new List<Vector2>();
                for (int i = 0; i < comp.DataCount; i++)
                    uvs.Add(reader.ReadVec2());

                header.AddUVComponent(Components.IndexOf(comp), uvs);
            }
        }

        public override void Write(FileWriter writer, HsfFile header)
        {
            var meshes = header.Meshes.Where(x => x.TexCoords.Count > 0).ToList();

            long posStart = writer.Position;
            foreach (var mesh in meshes)
            {
                writer.Write(header.GetStringOffset(mesh.Name));
                writer.Write(mesh.TexCoords.Count);
                writer.Write(uint.MaxValue);
            }

            long dataPos = writer.Position;
            for (int i = 0; i < meshes.Count; i++)
            {
                meshes[i].ObjectData.TexCoordIndex = i;

                writer.Align(0x20);
                writer.WriteUint32Offset(posStart + 8 + (i * 12), dataPos);
                for (int j = 0; j < meshes[i].TexCoords.Count; j++)
                    writer.Write(meshes[i].TexCoords[j]);
            }
            writer.Align(4);
        }
    }

}
