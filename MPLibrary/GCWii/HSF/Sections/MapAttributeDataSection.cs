using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using System.Runtime.InteropServices;

namespace MPLibrary.GCN
{
    public class MapAttributeDataSection : HSFSection
    {
        public List<MapAttr> Attributes = new List<MapAttr>();

        public override void Read(FileReader reader, HsfFile header)
        {
            for (int i = 0; i < this.Count; i++)
            {
                MapAttr attr = new MapAttr();
                Attributes.Add(attr);

                attr.MinX = reader.ReadSingle();
                attr.MinZ = reader.ReadSingle();
                attr.MaxX = reader.ReadSingle();
                attr.MaxZ = reader.ReadSingle();

                uint data_idx = reader.ReadUInt32();
                uint data_count = reader.ReadUInt32();

                using (reader.TemporarySeek(this.Offset + this.Count * 24 + data_idx * 2, System.IO.SeekOrigin.Begin))
                {
                    attr.Indices = reader.ReadUInt16s((int)data_count);
                }
            }
        }

        public override void Write(FileWriter writer, HsfFile header)
        {
            int data_idx = 0;
            foreach (var map_attr in Attributes)
            {
                writer.Write(map_attr.MinX);
                writer.Write(map_attr.MinZ);
                writer.Write(map_attr.MaxX);
                writer.Write(map_attr.MaxZ);
                writer.Write(data_idx);
                writer.Write(map_attr.Indices.Length);

                data_idx += map_attr.Indices.Length;
            }

            foreach (var map_attr in Attributes)
                writer.Write(map_attr.Indices);

            writer.AlignBytes(4);
        }
    }

    public class MapAttr
    {
        public float MinX;
        public float MinZ;
        public float MaxX;
        public float MaxZ;

        public ushort[] Indices;
    }
}
