using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;
using System.Runtime.InteropServices;
using OpenTK;

namespace MPLibrary.GCN
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
    public class ObjectData
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

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] Unks;
        public int Unknown2;

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

    public class ObjectDataNode
    {
        public List<ObjectDataNode> Children = new List<ObjectDataNode>();

        public ObjectDataNode Parent;
        public ObjectData ObjectData;

        public ObjectDataNode(ObjectData obj) {
            ObjectData = obj;
        }
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

        /// <summary>
        /// Creates an empty object 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ObjectData CreateNewObject(ObjectType type)
        {
            ObjectData objectData = new ObjectData();
            objectData.Type = type;
            objectData.ConstDataOffset = 0; //Set at runtime
            objectData.RenderFlags = 0;
            objectData.ParentIndex = -1;
            objectData.ChildrenCount = 0;
            objectData.SymbolIndex = 0;
            objectData.BaseTransform = new Transform()
            {
                Translate = new Vector3XYZ(),
                Scale = new Vector3XYZ() { X = 1, Y = 1, Z = 1, },
                Rotate = new Vector3XYZ(),
            };
            objectData.CurrentTransform = new Transform() //Set at runtime
            {
                Translate = new Vector3XYZ(),
                Scale = new Vector3XYZ(),
                Rotate = new Vector3XYZ(),
            };
            objectData.Unknown2 = -1;
            objectData.Unks = new byte[128];
            objectData.CullBoxMin = new Vector3XYZ();
            objectData.CullBoxMax = new Vector3XYZ();
            objectData.FaceIndex = -1;
            objectData.VertexIndex = -1;
            objectData.NormalIndex = -1;
            objectData.ColorIndex = -1;
            objectData.TexCoordIndex = -1;
            objectData.MaterialDataOffset = 0; //Set at runtime
            objectData.AttributeIndex = -1;
            objectData.Unknown3 = 0;
            objectData.VertexChildCount = 0;
            objectData.VertexSymbolIndex = -1;
            objectData.CluserCount = 0;
            objectData.CluserSymbolIndex = -1;
            objectData.HookFlag = 0;
            objectData.CenvIndex = -1;
            objectData.PositionsOffset = 0;
            objectData.NormalsOffset = 0;

            return objectData;
        }

        internal int[] GenerateSymbols(int startIndex)
        {
            List<int> symbols = new List<int>();

            int index = startIndex + 1;
            for (int i = 0; i < Objects.Count; i++)
            {
                if (Objects[i].ChildrenCount > 0 && Objects[i].ChildrenCount < Objects.Count)
                {
                    for (int j = 0; j < Objects.Count; j++)
                    {
                        if (Objects[j].ParentIndex == i)
                            symbols.Add(j);
                    }
                }

                if (Objects[i].ChildrenCount < Objects.Count)
                {
                    Objects[i].SymbolIndex = index;
                    index += Objects[i].ChildrenCount;
                }
            }

            return symbols.ToArray();
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
