using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using System.Runtime.InteropServices;

namespace MPLibrary.GCN
{
    public class PartDataSection : HSFSection
    {
        public List<Part> Parts = new List<Part>();

        public override void Read(FileReader reader, HsfFile header)
        {
            for (int i = 0; i < this.Count; i++)
            {
                string name = header.GetString(reader, reader.ReadUInt32());
                uint num_vertices = reader.ReadUInt32();
                uint data_idx = reader.ReadUInt32();

                using (reader.TemporarySeek(this.Offset + this.Count * 12 + data_idx * 2, System.IO.SeekOrigin.Begin))
                {
                    Parts.Add(new Part()
                    {
                        Name = name,
                        VertexIndices = reader.ReadUInt16s((int)num_vertices),
                    });
                }
            }
        }

        public override void Write(FileWriter writer, HsfFile header)
        {
            int data_idx = 0;
            for (int i = 0; i < Parts.Count; i++)
            {
                writer.Write(header.GetStringOffset(Parts[i].Name));
                writer.Write(Parts[i].VertexIndices.Length);
                writer.Write(data_idx);

                data_idx += Parts[i].VertexIndices.Length;
            }

            for (int i = 0; i < Parts.Count; i++)
                writer.Write(Parts[i].VertexIndices);

            writer.AlignBytes(4);
        }
    }

    public class Part
    {
        public string Name;

        public ushort[] VertexIndices = new ushort[0];
    }
}
