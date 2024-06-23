using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MPLibrary.MP10.IO;

namespace MPLibrary.MP10
{
    internal class BnfmVertexBuffer
    {
        public static BnfmVertex[] ReadBuffer(BnfmMesh mesh, BnfmPolygon poly, FileReader reader, long bufferOffset)
        {
            BnfmVertex[] vertices = new BnfmVertex[poly.VertexCount];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new BnfmVertex();
                vertices[i].Position = ReadVector3(mesh.AttributeList.PositionData, reader, i, bufferOffset);
                vertices[i].Normal = ReadVector4(mesh.AttributeList.NormalData, reader, i, bufferOffset);
                vertices[i].Color = ReadVector4(mesh.AttributeList.ColorData, reader, i, bufferOffset);
                vertices[i].TexCoord0 = ReadVector2(mesh.AttributeList.TexCoordData0, reader, i, bufferOffset);
                vertices[i].TexCoord1 = ReadVector2(mesh.AttributeList.TexCoordData1, reader, i, bufferOffset);
                vertices[i].TexCoord2 = ReadVector2(mesh.AttributeList.TexCoordData2, reader, i, bufferOffset);
                vertices[i].Tangent = ReadVector4(mesh.AttributeList.Tangent, reader, i, bufferOffset);
                vertices[i].Bitangent = ReadVector4(mesh.AttributeList.Bitangent, reader, i, bufferOffset);
                vertices[i].BoneIndices = ReadVector4(mesh.AttributeList.BoneIndices, reader, i, bufferOffset);
                vertices[i].BoneWeights = ReadVector4(mesh.AttributeList.BoneWeights, reader, i, bufferOffset);

                vertices[i].Normal = Vector4.Normalize(vertices[i].Normal);
            }
            return vertices;
        }

        static Vector2 ReadVector2(BnfmAttribute att, FileReader reader, int index, long startOffset)
        {
            if (att.Format == 0) return new Vector2();

            reader.Seek(startOffset + att.VertexOffset + (index * att.TotalVertexStride), System.IO.SeekOrigin.Begin);
            return new Vector2(
                ParseValue(reader, att.Format),
                ParseValue(reader, att.Format));
        }

        static Vector3 ReadVector3(BnfmAttribute att, FileReader reader, int index, long startOffset)
        {
            if (att.Format == 0) return new Vector3();

            reader.Seek(startOffset + att.VertexOffset + (index * att.TotalVertexStride), System.IO.SeekOrigin.Begin);
            return new Vector3(
                ParseValue(reader, att.Format), 
                ParseValue(reader, att.Format),
                ParseValue(reader, att.Format));
        }

        static Vector4 ReadVector4(BnfmAttribute att, FileReader reader, int index, long startOffset)
        {
            if (att.Format == 0) return new Vector4();

            reader.Seek(startOffset + att.VertexOffset + (index * att.TotalVertexStride), System.IO.SeekOrigin.Begin);
            return new Vector4(
                ParseValue(reader, att.Format),
                ParseValue(reader, att.Format),
                ParseValue(reader, att.Format),
                ParseValue(reader, att.Format));
        }

        static float ParseValue(FileReader reader, BnfmAttribute.AttributeFormat format)
        {
            switch (format)
            {
                case BnfmAttribute.AttributeFormat.Float: return reader.ReadSingle();
                case BnfmAttribute.AttributeFormat.Byte: return reader.ReadByte();
                case BnfmAttribute.AttributeFormat.Snorm8: return reader.ReadSByte() / 127f;
                case BnfmAttribute.AttributeFormat.Unorm8: return reader.ReadByte()  / 255f;
                case BnfmAttribute.AttributeFormat.HalfSingle: return reader.ReadHalfSingle();
                case BnfmAttribute.AttributeFormat.Sbyte: return reader.ReadSByte();
                default:
                    return reader.ReadSingle();
            }
        }

        public static void CalculateAttributeOffsets(List<BnfmModel> models)
        {
            uint offset = 0;
            uint totalOffset = 0;

            void ApplyOffset(BnfmAttribute attribute, uint numElements)
            {
                if (attribute.Format == 0)
                    return;

                attribute.VertexOffset = offset;
                offset += GetStride(attribute.Format) * numElements;
            };
            foreach (var model in models)
            {
                foreach (var mesh in model.Meshes)
                {
                    offset = totalOffset;

                    ApplyOffset(mesh.AttributeList.PositionData, 3);
                    ApplyOffset(mesh.AttributeList.NormalData, 4);
                    ApplyOffset(mesh.AttributeList.ColorData, 4);
                    ApplyOffset(mesh.AttributeList.TexCoordData0, 2);
                    ApplyOffset(mesh.AttributeList.TexCoordData1, 2);
                    ApplyOffset(mesh.AttributeList.TexCoordData2, 2);
                    ApplyOffset(mesh.AttributeList.BoneIndices, 4);
                    ApplyOffset(mesh.AttributeList.BoneWeights, 4);
                    ApplyOffset(mesh.AttributeList.Tangent, 4);
                    ApplyOffset(mesh.AttributeList.Bitangent, 4);

                    foreach (var poly in mesh.Polygons)
                    {
                        poly.VertexCount = (uint)poly.Vertices.Length;
                        totalOffset += (uint)(poly.VertexCount * mesh.AttributeList.PositionData.TotalVertexStride);
                    }
                }
            }
        }

        public static void WriteBuffer(FileWriter writer, BnfmMesh mesh, BnfmVertex[] vertices)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                WriteVector3(mesh.AttributeList.PositionData, writer, vertices[i].Position);
                WriteVector4(mesh.AttributeList.NormalData, writer, vertices[i].Normal);
                WriteVector4(mesh.AttributeList.ColorData, writer, vertices[i].Color);
                WriteVector2(mesh.AttributeList.TexCoordData0, writer, vertices[i].TexCoord0);
                WriteVector2(mesh.AttributeList.TexCoordData1, writer, vertices[i].TexCoord1);
                WriteVector2(mesh.AttributeList.TexCoordData2, writer, vertices[i].TexCoord2);
                WriteVector4(mesh.AttributeList.BoneIndices, writer, vertices[i].BoneIndices);
                WriteVector4(mesh.AttributeList.BoneWeights, writer, vertices[i].BoneWeights);
                WriteVector4(mesh.AttributeList.Tangent, writer, vertices[i].Tangent);
                WriteVector4(mesh.AttributeList.Bitangent, writer, vertices[i].Bitangent);
            }
        }

        static void WriteVector2(BnfmAttribute att, FileWriter writer, Vector2 value)
        {
            WriteValue(writer, att.Format, value.X);
            WriteValue(writer, att.Format, value.Y);
        }

        static void WriteVector3(BnfmAttribute att, FileWriter writer, Vector3 value)
        {
            WriteValue(writer, att.Format, value.X);
            WriteValue(writer, att.Format, value.Y);
            WriteValue(writer, att.Format, value.Z);
        }

        static void WriteVector4(BnfmAttribute att, FileWriter writer, Vector4 value)
        {
            WriteValue(writer, att.Format, value.X);
            WriteValue(writer, att.Format, value.Y);
            WriteValue(writer, att.Format, value.Z);
            WriteValue(writer, att.Format, value.W);
        }

        static void WriteValue(FileWriter writer, BnfmAttribute.AttributeFormat format, float value)
        {
            if (format == 0)
                return;

            switch (format)
            {
                case BnfmAttribute.AttributeFormat.Float:
                    writer.Write(value);
                    break;
                case BnfmAttribute.AttributeFormat.Byte:
                    writer.Write((byte)value);
                    break;
                case BnfmAttribute.AttributeFormat.Snorm8:
                    writer.Write((sbyte)(value * 127));
                    break;
                case BnfmAttribute.AttributeFormat.Unorm8:
                    writer.Write((byte)(value * 255));
                    break;
                case BnfmAttribute.AttributeFormat.HalfSingle:
                    writer.WriteHalfFloat(value);
                    break;
                case BnfmAttribute.AttributeFormat.Sbyte:
                    writer.Write((sbyte)value);
                    break;
                default:
                    writer.Write(value);
                    break;
            }
        }

        static uint GetStride(BnfmAttribute.AttributeFormat format)
        {
            switch (format)
            {
                case BnfmAttribute.AttributeFormat.Float: return 4;
                case BnfmAttribute.AttributeFormat.Byte: return 1;
                case BnfmAttribute.AttributeFormat.Snorm8: return 1;
                case BnfmAttribute.AttributeFormat.Unorm8: return 1;
                case BnfmAttribute.AttributeFormat.HalfSingle: return 2;
                case BnfmAttribute.AttributeFormat.Sbyte: return 1;
                default: return 4;
            }
        }
    }

    public struct BnfmVertex
    {
        public Vector3 Position;
        public Vector4 Normal;
        public Vector4 Color;
        public Vector2 TexCoord0;
        public Vector2 TexCoord1;
        public Vector2 TexCoord2;
        public Vector4 Tangent;
        public Vector4 Bitangent;
        public Vector4 BoneIndices;
        public Vector4 BoneWeights;

        public int GetIndex(int id)
        {
            if (id == 0)      return (int)BoneIndices.X;
            else if (id == 1) return (int)BoneIndices.Y;
            else if (id == 2) return (int)BoneIndices.Z;
                              return (int)BoneIndices.W;
        }

        public void SetBoneIndex(int id, int index)
        {
            if (id == 0)      BoneIndices.X = index;
            else if (id == 1) BoneIndices.Y = index;
            else if (id == 2) BoneIndices.Z = index;
            else if (id == 3) BoneIndices.W = index;
        }
        public float GetWeight(int id)
        {
            if (id == 0)      return BoneWeights.X;
            else if (id == 1) return BoneWeights.Y;
            else if (id == 2) return BoneWeights.Z;
                              return BoneWeights.W;
        }
    }
}
