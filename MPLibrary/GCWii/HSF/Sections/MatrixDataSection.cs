using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;
using System.Runtime.InteropServices;

namespace MPLibrary.GCN
{
    public class MatrixDataSection : HSFSection
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Matrix
        {
            public Vector3XYZ Zero;
            public uint Unknown; //0x06
            public uint Flags1; //First byte an index for mesh in combined matrix list, else 0xCCCCCCCC
            public uint Flags2; //First byte an index
            public uint Unknown1;
            public uint Unknown2;
            public uint Unknown3;
            public uint Unknown4;
            public uint Unknown5;
            public uint Unknown6;
        }

        public List<Matrix> MeshMatrices = new List<Matrix>();
        public List<Matrix> NodeMatrices = new List<Matrix>();
        public List<Matrix> CombinedMatrices = new List<Matrix>();

        public override void Read(FileReader reader, HsfFile header)
        {
            for (int i = 0; i < this.Count; i++)
            {
                uint numMeshMatrices = reader.ReadUInt32();
                uint numNodeMatrices = reader.ReadUInt32();
                uint unk = reader.ReadUInt32(); //another matrix count? Always 0
                MeshMatrices = reader.ReadMultipleStructs<Matrix>((int)numMeshMatrices);
                NodeMatrices = reader.ReadMultipleStructs<Matrix>((int)numNodeMatrices);
                CombinedMatrices = reader.ReadMultipleStructs<Matrix>((int)(numNodeMatrices * numMeshMatrices));

            }

            for (int i = 0; i < MeshMatrices.Count; i++)
                LoadMatrix(MeshMatrices[i]);
            for (int i = 0; i < NodeMatrices.Count; i++)
                LoadMatrix(NodeMatrices[i]);
            for (int i = 0; i < CombinedMatrices.Count; i++)
                LoadMatrix(CombinedMatrices[i]);
        }

        private void LoadMatrix(Matrix mat)
        {
            int index2 = (int)mat.Flags1 >> 24;
            int index = (int)mat.Flags2 >> 24;

         //   Console.WriteLine($"{mat.Unknown1} {mat.Unknown2} {mat.Unknown3} {mat.Unknown4} {mat.Unknown5} {mat.Unknown6}");
        }

        public override void Write(FileWriter writer, HsfFile header)
        {
            writer.Write(MeshMatrices.Count);
            writer.Write(NodeMatrices.Count);
            writer.Write(0);

            for (int i = 0; i < MeshMatrices.Count; i++)
                writer.WriteStruct(MeshMatrices[i]);
            for (int i = 0; i < NodeMatrices.Count; i++)
                writer.WriteStruct(NodeMatrices[i]);
            for (int i = 0; i < CombinedMatrices.Count; i++)
                writer.WriteStruct(CombinedMatrices[i]);
        }
    }

}
