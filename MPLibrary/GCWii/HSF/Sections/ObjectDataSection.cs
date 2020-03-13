using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Library.IO;
using System.Runtime.InteropServices;
using OpenTK;

namespace MPLibrary
{
    public enum ObjectType : int
    {
       Root = 0,
       Mesh = 2,
       BonesNoSkinnning = 3,
       BonesSkinning = 4,
       Effect = 5,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ObjectData
    {
        public uint StringOffset;
        public ObjectType Type;
        public int ConstDataOffset;
        public int RenderFlags;
        public int ParentIndex;
        public int ChildrenCount;
        public int SymbolIndex;
        public Transform BaseTransform;
        public Transform CurrentTransform;
        public Vector3XYZ CullBoxMin;
        public Vector3XYZ CullBoxMax;
        public float Unk;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 132)]
        public byte[] Unks;

        public int FaceIndex;
        public int VertexIndex;
        public int NormalIndex;
        public int ColorIndex;
        public int TexCoordIndex;
        public int MaterialDataOffset;
        public int AttributeIndex;
        public int Unknown3;
        public int VertexChildCount;
        public int VertexSymbolIndex;
        public int CluserCount;
        public int CluserSymbolIndex;
        public int HookFlag;
        public int CenvIndex;
        public int PositionsOffset;
        public int NormalsOffset;
    }


    public class ObjectDataSection : HSFSection
    {
        public List<ObjectData> Objects = new List<ObjectData>();
        public List<string> ObjectNames = new List<string>();

        public List<EffectMesh> Meshes = new List<EffectMesh>();

        public override void Read(FileReader reader, HsfFile header)
        {
            Objects = reader.ReadMultipleStructs<ObjectData>(this.Count);
            for (int i = 0; i < Objects.Count; i++)
                ObjectNames.Add(header.GetString(reader, Objects[i].StringOffset));
        }

        internal void ReadEffectMeshes(FileReader reader, HsfFile header)
        {
            //Read additional custom effect meshes
            List<uint> readMeshes = new List<uint>();
            for (int i = 0; i < Objects.Count; i++)
            {
                if (Objects[i].PositionsOffset == 0 || Objects[i].VertexIndex < 0 ||
                    readMeshes.Contains((uint)Objects[i].PositionsOffset))
                    continue;

                EffectMesh mesh = new EffectMesh();
                Meshes.Add(mesh);

                readMeshes.Add((uint)Objects[i].PositionsOffset);

                var comp = header.PositionData.Components[Objects[i].VertexIndex];
                mesh.Name = header.GetString(reader, comp.StringOffset);
                using (reader.TemporarySeek(Objects[i].PositionsOffset, System.IO.SeekOrigin.Begin))
                {
                    for (int j = 0; j < comp.DataCount; j++)
                        mesh.Positions.Add(reader.ReadVec3());
                }

                if (Objects[i].NormalIndex >= 0)
                {
                    var normalData = header.NormalData.Components[Objects[i].NormalIndex];
                    using (reader.TemporarySeek(Objects[i].NormalsOffset, System.IO.SeekOrigin.Begin))
                    {
                        for (int j = 0; j < comp.DataCount; j++)
                            mesh.Normals.Add(reader.ReadVec3());
                    }
                }
            }
        }

        public override void Write(FileWriter writer, HsfFile header)
        {
            for (int i = 0; i < Objects.Count; i++)
            {
                var obj = Objects[i];
                obj.StringOffset = (uint)header.GetStringOffset(ObjectNames[i]);
                writer.WriteStruct(obj);
            }
        }

        public void WriteEffectPositions(FileWriter writer, HsfFile header)
        {
            if (Meshes.Count == 0)
                return;

            List<EffectMesh> readMeshes = new List<EffectMesh>();

            var ObjectsSorted = Objects.OrderBy(x => x.VertexIndex).ToList();

            for (int i = 0; i < ObjectsSorted.Count; i++)
            {
                if (ObjectsSorted[i].VertexIndex >= 0 && Meshes.Count > ObjectsSorted[i].VertexIndex)
                {
                    var effectMesh = Meshes[ObjectsSorted[i].VertexIndex];
                    int index = Objects.IndexOf(ObjectsSorted[i]);
                    if (!readMeshes.Contains(effectMesh))
                    {
                        writer.Align(0x20);
                        effectMesh.PositionOffset = (uint)writer.Position;
                        for (int j = 0; j < effectMesh.Positions.Count; j++)
                            writer.Write(effectMesh.Positions[j]);

                        readMeshes.Add(effectMesh);
                    }
                    var obj = Objects[index];
                    obj.PositionsOffset = (int)effectMesh.PositionOffset;
                }
            }
        }

        public void WriteEffectNormals(FileWriter writer, HsfFile header)
        {
            if (Meshes.Count == 0)
                return;

            List<EffectMesh> readMeshes = new List<EffectMesh>();

            var ObjectsSorted = Objects.OrderBy(x => x.VertexIndex).ToList();

            for (int i = 0; i < ObjectsSorted.Count; i++)
            {
                if (ObjectsSorted[i].VertexIndex >= 0 && Meshes.Count > ObjectsSorted[i].VertexIndex)
                {
                    var effectMesh = Meshes[ObjectsSorted[i].VertexIndex];
                    int index = Objects.IndexOf(ObjectsSorted[i]);
                    if (!readMeshes.Contains(effectMesh))
                    {
                        writer.Align(0x20);
                        effectMesh.NormalOffset = (uint)writer.Position;
                        for (int j = 0; j < effectMesh.Normals.Count; j++)
                            writer.Write(effectMesh.Normals[j]);

                        readMeshes.Add(effectMesh);
                    }
                    var obj = Objects[index];
                    obj.NormalsOffset = (int)effectMesh.NormalOffset;
                }
            }
        }
    }
}
