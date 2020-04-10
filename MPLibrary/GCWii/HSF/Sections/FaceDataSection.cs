using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;
using System.Runtime.InteropServices;
using OpenTK;

namespace MPLibrary
{
    public class PrimitiveObject
    {
        public PrimitiveType Type;
        public int MaterialIndex;

        public int FlagValue = 8;

        public VertexGroup[] Vertices;

        public ushort Flags;

        public int TriCount;

        public Vector3 NbtData;
    }

    public class FaceDataSection : HSFSection
    {
        public override void Read(FileReader reader, HsfFile header)
        {
            List<ComponentData> Components = reader.ReadMultipleStructs<ComponentData>(this.Count);
            long pos = reader.Position;

            var ExtOffset = pos;
            foreach (var att in Components)
                ExtOffset += att.DataCount * 48;
            foreach (var comp in Components) {
                List<PrimitiveObject> primatives = new List<PrimitiveObject>();
                reader.SeekBegin(pos + comp.DataOffset);
                for (int i = 0; i < comp.DataCount; i++) {
                    var prim = new PrimitiveObject();
                    primatives.Add(prim);

                    prim.Type = (PrimitiveType)reader.ReadUInt16();
                    prim.Flags = reader.ReadUInt16();
                    prim.MaterialIndex = prim.Flags & 0xFFF;
                    prim.FlagValue = prim.Flags >> 12;

                    int primCount = 3;
                    if (prim.Type == PrimitiveType.Triangle || prim.Type == PrimitiveType.Quad)
                        primCount = 4;

                    prim.Vertices = reader.ReadMultipleStructs<VertexGroup>(primCount).ToArray();
                    if (prim.Type == PrimitiveType.TriangleStrip) {
                        primCount = reader.ReadInt32();
                        var offset = reader.ReadUInt32();
                        var temp = reader.Position;
                        reader.Position = ExtOffset + offset * 8;

                        var verts = reader.ReadMultipleStructs<VertexGroup>(primCount).ToArray();
                        reader.Position = temp;

                        prim.TriCount = prim.Vertices.Length;
                        var newVert = new VertexGroup[prim.Vertices.Length + primCount + 1];
                        Array.Copy(prim.Vertices, 0, newVert, 0, prim.Vertices.Length);
                        newVert[3] = newVert[1];
                        Array.Copy(verts, 0, newVert, prim.Vertices.Length + 1, verts.Length);
                        prim.Vertices = newVert;
                    }
                    prim.NbtData = reader.ReadVec3();
                }

                header.AddPrimitiveComponent(Components.IndexOf(comp), primatives);
            }
        }

        public override void Write(FileWriter writer, HsfFile header)
        {
            var meshes = header.Meshes.Where(x => x.Primitives.Count > 0).ToList();

            long posStart = writer.Position;

            foreach (var mesh in meshes)
            {
                writer.Write(header.GetStringOffset(mesh.Name));
                writer.Write(mesh.Primitives.Count);
                writer.Write(uint.MaxValue);
            }


            long dataStart = writer.Position;

            var ExtOffset = dataStart;
            foreach (var mesh in meshes)
                ExtOffset += mesh.Primitives.Count * 48;
            var triangleStripPosition = ExtOffset;

            int meshIndex = 0;
            long dataPos = writer.Position;
            foreach (var mesh in meshes)
            {
                writer.WriteUint32Offset(posStart + 8 + (meshIndex * 12), dataPos);
                foreach (var primitive in mesh.Primitives)
                {
                    writer.Write((ushort)primitive.Type);
                    primitive.Flags = (ushort)(primitive.MaterialIndex);
                    primitive.Flags |= (ushort)(primitive.FlagValue << 12);

                    writer.Write(primitive.Flags);

                    int primCount = 3;
                    if (primitive.Type == PrimitiveType.Triangle || primitive.Type == PrimitiveType.Quad)
                        primCount = 4;

                    for (int i = 0; i < primCount; i++)
                        writer.WriteStruct(primitive.Vertices[i]);

                    if (primitive.Type == PrimitiveType.TriangleStrip)
                    {
                        writer.Write((uint)(primitive.Vertices.Length - 4));
                        long stripOffsetPos = writer.Position;
                        long offset = triangleStripPosition - ExtOffset;
                        if (offset != 0)
                            offset /= 8;
                        writer.Write((uint)offset);

                        using (writer.TemporarySeek(triangleStripPosition, System.IO.SeekOrigin.Begin))
                        {
                            for (int i = 4; i < primitive.Vertices.Length; i++)
                                writer.WriteStruct(primitive.Vertices[i]);

                            triangleStripPosition = writer.Position;
                        }
                    }
                    writer.Write(primitive.NbtData);
                }
                meshIndex++;
            }

            writer.SeekBegin(triangleStripPosition);
        }
    }

}
