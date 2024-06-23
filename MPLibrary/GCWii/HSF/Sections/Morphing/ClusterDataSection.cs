using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using System.Runtime.InteropServices;

namespace MPLibrary.GCN
{
    public class ClusterDataSection : HSFSection
    {       
        public List<HSFCluster> Clusters = new List<HSFCluster>();

        public override void Read(FileReader reader, HsfFile header)
        {
            int[] GetIndices()
            {
                uint num = reader.ReadUInt32();
                uint idx = reader.ReadUInt32();

                int[] indices = new int[num];
                for (int i = 0; idx < num; i++)
                    indices[i] = header.SymbolData.SymbolIndices[idx + i];

                return indices;
            }

            for (int i = 0; i < this.Count; i++)
            {
                string name_1 = header.GetString(reader, reader.ReadUInt32());
                string name_2 = header.GetString(reader, reader.ReadUInt32());
                string target_name = header.GetString(reader, reader.ReadUInt32());

                Clusters.Add(new HSFCluster()
                {
                    Name_1 = name_1, Name_2 = name_2,
                    PartTarget = target_name,
                    PartIdx = reader.ReadUInt32(),
                    BaseMorph = reader.ReadSingle(),
                    Weights = reader.ReadSingles(32),
                    IsAdjusted = reader.ReadBoolean(),
                    Unknown = reader.ReadByte(),
                    Type = reader.ReadUInt16(),
                    BufferCount = reader.ReadInt32(),
                    BufferSymbolIdx = reader.ReadInt32(),
                });
            }
        }

        public override void Write(FileWriter writer, HsfFile header)
        {
            for (int i = 0; i < Clusters.Count; i++)
            {
                writer.Write(header.GetStringOffset(Clusters[i].Name_1));
                writer.Write(header.GetStringOffset(Clusters[i].Name_2));
                writer.Write(header.GetStringOffset(Clusters[i].PartTarget));
                writer.Write(Clusters[i].PartIdx);
                writer.Write(Clusters[i].BaseMorph);
                writer.Write(Clusters[i].Weights);
                writer.Write((bool)Clusters[i].IsAdjusted);
                writer.Write((byte)Clusters[i].Unknown);
                writer.Write((ushort)Clusters[i].Type);
                writer.Write(Clusters[i].BufferIndices.Length);
                writer.Write(Clusters[i].BufferSymbolIdx);
            }
            writer.AlignBytes(4);
        }

        public List<string> GetStrings()
        {
            var name_list = new List<string>();
            name_list.AddRange(this.Clusters.Select(x => x.Name_1));
            name_list.AddRange(this.Clusters.Select(x => x.Name_2));
            name_list.AddRange(this.Clusters.Select(x => x.PartTarget));
            return name_list;
        }
    }

    public class HSFCluster
    {
        public string Name_1;
        public string Name_2;

        public string PartTarget;

        public uint PartIdx;

        public float BaseMorph;

        public float[] Weights = new float[32];

        public bool IsAdjusted = false;

        public byte Unknown = 150;
        public ushort Type = 0;

        internal int BufferSymbolIdx;
        internal int BufferCount;

        public int[] BufferIndices = new int[0];
    }
}
