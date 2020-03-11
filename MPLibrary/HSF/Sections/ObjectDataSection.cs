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
        public int FileOffset1;
        public int FileOffset2;
    }


    public class ObjectDataSection : HSFSection
    {
        public List<ObjectData> Objects = new List<ObjectData>();
        public List<string> ObjectNames = new List<string>();

        public override void Read(FileReader reader, HsfFile header) {
            Objects = reader.ReadMultipleStructs<ObjectData>(this.Count);
            for (int i = 0; i < Objects.Count; i++) {
                ObjectNames.Add(header.GetString(reader, Objects[i].StringOffset));

                Console.WriteLine($"{Objects[i].Type} {ObjectNames[i]} {Objects[i].FileOffset1}");
            }
            List<ObjectData> orderedFile1 = Objects.OrderBy(x => x.FileOffset1).ToList();
            for (int i = 0; i < orderedFile1.Count; i++)
            {
                if (i < orderedFile1.Count - 1 && orderedFile1[i + 1].FileOffset1 == orderedFile1[i].FileOffset1)
                    continue;

                int index = Objects.IndexOf(orderedFile1[i]);
                int size = 0;
                if (i < orderedFile1.Count - 1)
                    size = orderedFile1[i + 1].FileOffset1 - orderedFile1[i].FileOffset1;

                int posIndex = orderedFile1[i].VertexIndex;
                if (posIndex > 0)
                    Console.WriteLine($"{ObjectNames[index]} {orderedFile1[i].FileOffset1} size {size} vertex" +
                        $" {header.PositionData.Components[posIndex].DataCount * 12}");
            }
        }

        public override void Write(FileWriter writer, HsfFile header) {
            for (int i = 0; i < Objects.Count; i++) {
                var obj = Objects[i];
                obj.StringOffset = (uint)header.GetStringOffset(ObjectNames[i]);
                writer.WriteStruct(obj);
            }
        }
    }

}
