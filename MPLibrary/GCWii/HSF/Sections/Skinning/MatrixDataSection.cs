using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using System.Runtime.InteropServices;
using System.Reflection;

namespace MPLibrary.GCN
{
    public class MatrixDataSection : HSFSection
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Matrix
        {
            //3x4 matrix set at runtime
            public Vector3XYZ Row0;

            public uint Index0;
            public uint Index1;
            public uint Index2;

            public uint Unknown0;
            public uint Unknown1;
            public uint Unknown2;

            public uint Unknown3;
            public uint Unknown4;
            public uint Unknown5;

            public Matrix()
            {
                Row0 = new Vector3XYZ(0, 0, 0); //zero
                Index0 = 0;  //set to mesh count
                Index1 = 3435973836;
                Index2 = 0;
                Unknown0 = 3036549632;
                Unknown1 = 1689206784;
                Unknown2 = 3759229184;
                Unknown3 = 419369472;
                Unknown4 = 0;
                Unknown5 = 15793535;
            }

            public Matrix(uint index_0, uint index_1, uint index_2)
            {
                Row0 = new Vector3XYZ(0, 0, 0); //zero
                Index0 = index_0;  //set to mesh count
                Index1 = index_1; //index when using combined matrices
                Index2 = index_2; //set to current index
                Unknown0 = 3036549632;
                Unknown1 = 1689206784;
                Unknown2 = 3759229184;
                Unknown3 = 419369472;
                Unknown4 = 0;
                Unknown5 = 15793535;
            }
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

                reader.SetByteOrder(false);

                MeshMatrices = reader.ReadMultipleStructs<Matrix>((int)numMeshMatrices);
                NodeMatrices = reader.ReadMultipleStructs<Matrix>((int)numNodeMatrices);
                CombinedMatrices = reader.ReadMultipleStructs<Matrix>((int)(numNodeMatrices * numMeshMatrices));

                reader.SetByteOrder(true);
            }
        }

        public override void Write(FileWriter writer, HsfFile header)
        {
            writer.Write(MeshMatrices.Count);
            writer.Write(NodeMatrices.Count);
            writer.Write(0);

            writer.SetByteOrder(false);

            for (int i = 0; i < MeshMatrices.Count; i++)
                writer.WriteStruct(MeshMatrices[i]);
            for (int i = 0; i < NodeMatrices.Count; i++)
                writer.WriteStruct(NodeMatrices[i]);
            for (int i = 0; i < CombinedMatrices.Count; i++)
                writer.WriteStruct(CombinedMatrices[i]);

            writer.SetByteOrder(true);
        }

        public void Update(HsfFile header)
        {
            MeshMatrices = new List<Matrix>();
            NodeMatrices = new List<Matrix>();
            CombinedMatrices = new List<Matrix>();

            int num_meshes = header.Meshes.Count;

            //Note: matrices are automatically computed and calculated at runtime so we don't need to compute these
            for (int i = 0; i < num_meshes; i++)
                MeshMatrices.Add(new Matrix((uint)num_meshes, HsfGlobals.NULL, (uint)i));

            for (int i = 0; i < header.ObjectNodes.Count; i++)
                NodeMatrices.Add(new Matrix((uint)num_meshes, HsfGlobals.NULL, (uint)i));

            int index = 0;
            for (int i = 0; i < num_meshes; i++)
            {
                for (int j = 0; j < header.ObjectNodes.Count; j++)
                {
                    CombinedMatrices.Add(new Matrix((uint)num_meshes, (uint)index, (uint)i));
                    index++;
                }
            }

            this.Count = 1;
        }
    }
}
