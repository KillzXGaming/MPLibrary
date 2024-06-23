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
            public uint NameSymbol; //NULL
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

            List<HSFEnvelope> envelopes = new List<HSFEnvelope>();
            for (int i = 0; i < rigs.Count; i++)
            {
                if (rigs[i].SingleBindCount != 0)
                    reader.SeekBegin(pos + rigs[i].SingleBindOffset);
                var singleBinds = reader.ReadMultipleStructs<RiggingSingleBind>(rigs[i].SingleBindCount);

                if (rigs[i].DoubleBindCount != 0)
                    reader.SeekBegin(pos + rigs[i].DoubleBindOffset);
                var doubleBinds = reader.ReadMultipleStructs<RiggingDoubleBind>(rigs[i].DoubleBindCount);

                if (rigs[i].MultiBindCount != 0)
                    reader.SeekBegin(pos + rigs[i].MultiBindOffset);
                var multiBinds = reader.ReadMultipleStructs<RiggingMultiBind>(rigs[i].MultiBindCount);

                envelopes.Add(new HSFEnvelope()
                {
                    NameSymbol = rigs[i].NameSymbol,
                    SingleBinds = singleBinds,
                    DoubleBinds = doubleBinds,
                    MultiBinds = multiBinds,
                    VertexCount = rigs[i].VertexCount,
                    CopyCount = rigs[i].SingleBind,
                });
            }

            var weightStart = reader.Position;
            foreach (var cenv in envelopes)
            {
                foreach (var mb in cenv.DoubleBinds)
                {
                    reader.Position = (uint)(weightStart + mb.WeightOffset);
                    cenv.DoubleWeights.AddRange(reader.ReadMultipleStructs<RiggingDoubleWeight>(mb.Count));
                }
                foreach (var mb in cenv.MultiBinds)
                {
                    reader.Position = (uint)(weightStart + mb.WeightOffset);
                    cenv.MultiWeights.AddRange(reader.ReadMultipleStructs<RiggingMultiWeight>(mb.Count));
                }
            }

            //Apply envelopes to linked objects
            foreach (var mesh in header.Meshes)
            {
                if (mesh.ObjectData.CenvIndex != -1)
                {
                    mesh.Envelopes.Clear();
                    for (int i = 0; i < mesh.ObjectData.CenvCount; i++)
                        mesh.Envelopes.Add(envelopes[mesh.ObjectData.CenvIndex + i]);
                }
            }
        }

        public override void Write(FileWriter writer, HsfFile header)
        {
            var envelopes = new List<HSFEnvelope>();

            foreach (var obj in header.ObjectNodes)
            {
                if (obj.MeshData != null && obj.MeshData.HasEnvelopes)
                    envelopes.AddRange(obj.MeshData.Envelopes);
            }

            if (envelopes.Count > 0 && envelopes.Count != header.Meshes.Count)
                throw new Exception(); 

            long startPos = writer.Position;
            foreach (var cenv in envelopes)
            {
                writer.Write(cenv.NameSymbol);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(cenv.SingleBinds.Count);
                writer.Write(cenv.DoubleBinds.Count);
                writer.Write(cenv.MultiBinds.Count);
                writer.Write(cenv.VertexCount);
                writer.Write(cenv.CopyCount);
            }


            var bindWeightPosition = writer.Position;
            var doubleBindWeightSize = 0;
            for (int i = 0; i < envelopes.Count; i++) {
                bindWeightPosition += envelopes[i].SingleBinds.Count * 12;
                bindWeightPosition += envelopes[i].DoubleBinds.Count * 16;
                bindWeightPosition += envelopes[i].MultiBinds.Count * 16;
                foreach (var bind in envelopes[i].DoubleBinds)
                    doubleBindWeightSize += bind.Count * 12;
            }

            var doubleBindWeightsOffset = bindWeightPosition;
            var multiBindWeightsOffset = bindWeightPosition + doubleBindWeightSize;

            long indexDataPos = writer.Position;
            for (int i = 0; i < envelopes.Count; i++)
            {
                int doubleWeightIndex = 0;

                var cenv = envelopes[i];
                writer.WriteUint32Offset(startPos + 4 + (i * 36), indexDataPos);
                if (cenv.SingleBinds.Count > 0) {
                    foreach (var bind in cenv.SingleBinds)
                        writer.WriteStruct(bind);
                }

                writer.WriteUint32Offset(startPos + 8 + (i * 36), indexDataPos);
                if (cenv.DoubleBinds.Count > 0) {
                    foreach (var bind in cenv.DoubleBinds)
                    {
                        int index = cenv.DoubleBinds.IndexOf(bind);

                        writer.Write(bind.Bone1);
                        writer.Write(bind.Bone2);
                        writer.Write(bind.Count);
                        long weightPos = writer.Position;
                        writer.Write(uint.MaxValue);
                        if (index < cenv.DoubleWeights.Count)
                        {
                            using (writer.TemporarySeek(doubleBindWeightsOffset, System.IO.SeekOrigin.Begin)){

                                writer.WriteUint32Offset(weightPos, bindWeightPosition);
                                for (int j = 0; j < bind.Count; j++)
                                    writer.WriteStruct(cenv.DoubleWeights[doubleWeightIndex++]);
                                doubleBindWeightsOffset = writer.Position;
                            }
                        }
                    }
                }

                int multiWeightIndex = 0;

                writer.WriteUint32Offset(startPos + 12 + (i * 36), indexDataPos);
                if (cenv.MultiBinds.Count > 0) {
                    foreach (var bind in cenv.MultiBinds)
                    {
                        int index = cenv.MultiBinds.IndexOf(bind);

                        writer.Write(bind.Count);
                        writer.Write(bind.PositionIndex);
                        writer.Write(bind.PositionCount);
                        writer.Write(bind.NormalIndex);
                        writer.Write(bind.NormalCount);

                        long weightPos = writer.Position;
                        writer.Write(uint.MaxValue);
                        if (index < cenv.MultiWeights.Count)
                        {
                            using (writer.TemporarySeek(multiBindWeightsOffset, System.IO.SeekOrigin.Begin))
                            {
                                writer.WriteUint32Offset(weightPos, bindWeightPosition);
                                for (int j = 0; j < bind.Count; j++)
                                    writer.WriteStruct(cenv.MultiWeights[multiWeightIndex++]);
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
