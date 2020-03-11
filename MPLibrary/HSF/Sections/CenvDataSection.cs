using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Library.IO;
using System.Runtime.InteropServices;

namespace MPLibrary
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RiggingSingleBind
    {
        public int BoneIndex;
        public short PositionIndex;
        public short PositionCount;
        public short NormalIndex;
        public short NormalCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RiggingDoubleBind
    {
        public int Bone1;
        public int Bone2;
        public int Count;
        public int WeightOffset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RiggingMultiBind
    {
        public int Count;
        public short PositionIndex;
        public short PositionCount;
        public short NormalIndex;
        public short NormalCount;
        public int WeightOffset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RiggingDoubleWeight
    {
        public float Weight;
        public short PositionIndex;
        public short PositionCount;
        public short NormalIndex;
        public short NormalCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RiggingMultiWeight
    {
        public int BoneIndex;
        public float Weight;
    }

    public class CenvDataSection : HSFSection
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct RiggeObject
        {
            public uint Unknown; //0xCCCCCCCC
            public uint SingleBindOffset;
            public uint DoubleBindOffset;
            public uint MultiBindOffset;
            public uint SingleBindCount;
            public uint DoubleBindCount;
            public uint MultiBindCount;
            public uint VertexCount;
            public uint SingleBind;
        }

        public override void Read(FileReader reader, HsfFile header)
        {
            var rigs = reader.ReadMultipleStructs<RiggeObject>(this.Count);
            long pos = reader.Position;

            var meshes = header.Meshes.Keys.ToList();
            for (int i = 0; i < rigs.Count; i++)
            {
                var mesh = header.Meshes[meshes[i]];

                reader.SeekBegin(pos + rigs[i].SingleBindOffset);
                var singleBinds = reader.ReadMultipleStructs<RiggingSingleBind>(rigs[i].SingleBindCount);

                reader.SeekBegin(pos + rigs[i].DoubleBindOffset);
                var doubleBinds = reader.ReadMultipleStructs<RiggingDoubleBind>(rigs[i].DoubleBindCount);

                reader.SeekBegin(pos + rigs[i].MultiBindOffset);
                var multiBinds = reader.ReadMultipleStructs<RiggingMultiBind>(rigs[i].MultiBindCount);

                mesh.AddRigging(new RiggingInfo()
                {
                    SingleBinds = singleBinds,
                    DoubleBinds = doubleBinds,
                    MultiBinds = multiBinds,
                    SingleBind = rigs[i].SingleBind,
                });
            }

            var weightStart = reader.Position;
            foreach (var mesh in header.GetAllMeshes())
            {
                if (!mesh.HasRigging)
                    continue;

                foreach (var mb in mesh.RiggingInfo.DoubleBinds)
                {
                    reader.Position = (uint)(weightStart + mb.WeightOffset);
                    mesh.RiggingInfo.DoubleWeights.AddRange(reader.ReadMultipleStructs<RiggingDoubleWeight>(mb.Count));
                }

                foreach (var mb in mesh.RiggingInfo.MultiBinds)
                {
                    reader.Position = (uint)(weightStart + mb.WeightOffset);
                    mesh.RiggingInfo.MultiWeights.AddRange(reader.ReadMultipleStructs<RiggingMultiWeight>(mb.Count));
                }
            }
        }

        public override void Write(FileWriter writer, HsfFile header)
        {
            var meshes = header.GetAllMeshes().Where(x => x.HasRigging).ToList();

            foreach (var mesh in meshes)
            {
              
            }
        }
    }

}
