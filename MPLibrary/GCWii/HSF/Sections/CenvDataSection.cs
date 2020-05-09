using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using System.Runtime.InteropServices;

namespace MPLibrary.GCN
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

            List<RiggingInfo> infos = new List<RiggingInfo>();
            for (int i = 0; i < rigs.Count; i++)
            {
                reader.SeekBegin(pos + rigs[i].SingleBindOffset);
                var singleBinds = reader.ReadMultipleStructs<RiggingSingleBind>(rigs[i].SingleBindCount);

                reader.SeekBegin(pos + rigs[i].DoubleBindOffset);
                var doubleBinds = reader.ReadMultipleStructs<RiggingDoubleBind>(rigs[i].DoubleBindCount);

                reader.SeekBegin(pos + rigs[i].MultiBindOffset);
                var multiBinds = reader.ReadMultipleStructs<RiggingMultiBind>(rigs[i].MultiBindCount);

                infos.Add(new RiggingInfo()
                {
                    Unknown = rigs[i].Unknown,
                    SingleBinds = singleBinds,
                    DoubleBinds = doubleBinds,
                    MultiBinds = multiBinds,
                    VertexCount = rigs[i].VertexCount,
                    SingleBind = rigs[i].SingleBind,
                });
            }

            foreach (var mesh in header.Meshes)
            {
                if (mesh.ObjectData.CenvIndex != -1)
                    mesh.RiggingInfo = infos[mesh.ObjectData.CenvIndex];
            }

            var weightStart = reader.Position;
            foreach (var mesh in header.Meshes)
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
            var meshes = header.Meshes.Where(x => x.HasRigging).ToList();
            long startPos = writer.Position;
            foreach (var mesh in meshes)
            {
                writer.Write(mesh.RiggingInfo.Unknown);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(mesh.RiggingInfo.SingleBinds.Count);
                writer.Write(mesh.RiggingInfo.DoubleBinds.Count);
                writer.Write(mesh.RiggingInfo.MultiBinds.Count);
                writer.Write(mesh.RiggingInfo.VertexCount);
                writer.Write(mesh.RiggingInfo.SingleBind);
            }


            var bindWeightPosition = writer.Position;
            var doubleBindWeightSize = 0;
            for (int i = 0; i < meshes.Count; i++) {
                bindWeightPosition += meshes[i].RiggingInfo.SingleBinds.Count * 12;
                bindWeightPosition += meshes[i].RiggingInfo.DoubleBinds.Count * 16;
                bindWeightPosition += meshes[i].RiggingInfo.MultiBinds.Count * 16;
                foreach (var bind in meshes[i].RiggingInfo.DoubleBinds)
                    doubleBindWeightSize += bind.Count * 12;
            }

            var doubleBindWeightsOffset = bindWeightPosition;
            var multiBindWeightsOffset = bindWeightPosition + doubleBindWeightSize;

            long indexDataPos = writer.Position;
            for (int i = 0; i < meshes.Count; i++)
            {
                meshes[i].ObjectData.CenvIndex = i;

                int doubleWeightIndex = 0;

                var mesh = meshes[i];
                if (mesh.RiggingInfo.SingleBinds.Count > 0) {
                    writer.WriteUint32Offset(startPos + 4 + (i * 36), indexDataPos);
                    foreach (var bind in mesh.RiggingInfo.SingleBinds)
                        writer.WriteStruct(bind);
                }
                if (mesh.RiggingInfo.DoubleBinds.Count > 0) {
                    writer.WriteUint32Offset(startPos + 8 + (i * 36), indexDataPos);
                    foreach (var bind in mesh.RiggingInfo.DoubleBinds)
                    {
                        int index = mesh.RiggingInfo.DoubleBinds.IndexOf(bind);

                        writer.Write(bind.Bone1);
                        writer.Write(bind.Bone2);
                        writer.Write(bind.Count);
                        long weightPos = writer.Position;
                        writer.Write(uint.MaxValue);
                        if (index < mesh.RiggingInfo.DoubleWeights.Count)
                        {
                            using (writer.TemporarySeek(doubleBindWeightsOffset, System.IO.SeekOrigin.Begin)){

                                writer.WriteUint32Offset(weightPos, bindWeightPosition);
                                for (int j = 0; j < bind.Count; j++)
                                    writer.WriteStruct(mesh.RiggingInfo.DoubleWeights[doubleWeightIndex++]);
                                doubleBindWeightsOffset = writer.Position;
                            }
                        }
                    }
                }

                int multiWeightIndex = 0;
                if (mesh.RiggingInfo.MultiBinds.Count > 0) {
                    writer.WriteUint32Offset(startPos + 12 + (i * 36), indexDataPos);
                    foreach (var bind in mesh.RiggingInfo.MultiBinds)
                    {
                        int index = mesh.RiggingInfo.MultiBinds.IndexOf(bind);

                        writer.Write(bind.Count);
                        writer.Write(bind.PositionIndex);
                        writer.Write(bind.PositionCount);
                        writer.Write(bind.NormalIndex);
                        writer.Write(bind.NormalCount);

                        long weightPos = writer.Position;
                        writer.Write(uint.MaxValue);
                        if (index < mesh.RiggingInfo.MultiWeights.Count)
                        {
                            using (writer.TemporarySeek(multiBindWeightsOffset, System.IO.SeekOrigin.Begin))
                            {
                                writer.WriteUint32Offset(weightPos, bindWeightPosition);
                                for (int j = 0; j < bind.Count; j++)
                                    writer.WriteStruct(mesh.RiggingInfo.MultiWeights[multiWeightIndex++]);
                                multiBindWeightsOffset = writer.Position;
                            }
                        }
                    }
                }
            }
            writer.SeekBegin(multiBindWeightsOffset);
            writer.Align(4);
        }
    }

}
