using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using MPLibrary.MP10.IO;

namespace MPLibrary.MP10
{
    /// <summary>
    /// Represents an attribute list used for handling vertex data.
    /// Each mesh contains an attribute list.
    /// </summary>
    public class BnfmAttributeList : ListItem, IFileData
    {
        public BnfmAttribute PositionData;
        public BnfmAttribute NormalData;
        public BnfmAttribute ColorData;
        public BnfmAttribute TexCoordData0;
        public BnfmAttribute TexCoordData1;
        public BnfmAttribute TexCoordData2;
        public BnfmAttribute Tangent;
        public BnfmAttribute Bitangent;
        public BnfmAttribute BoneIndices;
        public BnfmAttribute BoneWeights;

        public BnfmAttributeList()
        {
            PositionData = new BnfmAttribute(BnfmAttribute.AttributeFormat.Float);
            NormalData = new BnfmAttribute(BnfmAttribute.AttributeFormat.Snorm8);
            ColorData = new BnfmAttribute(BnfmAttribute.AttributeFormat.Unorm8);
            TexCoordData0 = new BnfmAttribute(BnfmAttribute.AttributeFormat.HalfSingle);
            TexCoordData1 = new BnfmAttribute(BnfmAttribute.AttributeFormat.HalfSingle);
            TexCoordData2 = new BnfmAttribute(0);
            Tangent = new BnfmAttribute(BnfmAttribute.AttributeFormat.Sbyte);
            Bitangent = new BnfmAttribute(BnfmAttribute.AttributeFormat.Sbyte);
            BoneIndices = new BnfmAttribute(BnfmAttribute.AttributeFormat.Byte);
            BoneWeights = new BnfmAttribute(BnfmAttribute.AttributeFormat.Unorm8);
            Flag = 1;
        }

        void IFileData.Read(FileReader reader)
        {
            PositionData = reader.ReadSection<BnfmAttribute>();
            NormalData = reader.ReadSection<BnfmAttribute>();
            ColorData = reader.ReadSection<BnfmAttribute>();
            TexCoordData0 = reader.ReadSection<BnfmAttribute>();
            TexCoordData1 = reader.ReadSection<BnfmAttribute>();
            TexCoordData2 = reader.ReadSection<BnfmAttribute>();
            Tangent = reader.ReadSection<BnfmAttribute>();
            Bitangent = reader.ReadSection<BnfmAttribute>();
            BoneIndices = reader.ReadSection<BnfmAttribute>();
            BoneWeights = reader.ReadSection<BnfmAttribute>();
            Index = reader.ReadInt32();
            Flag = reader.ReadInt32();
        }

        void IFileData.Write(FileWriter writer)
        {
            writer.Write(PositionData);
            writer.Write(NormalData);
            writer.Write(ColorData);
            writer.Write(TexCoordData0);
            writer.Write(TexCoordData1);
            writer.Write(TexCoordData2);
            writer.Write(Tangent);
            writer.Write(Bitangent);
            writer.Write(BoneIndices);
            writer.Write(BoneWeights);
            writer.Write(Index);
            writer.Write(Flag);
        }
    }
    public class BnfmAttribute : IFileData
    {
        public AttributeFormat Format;
        public int TotalVertexStride; //Always 44?
        public uint VertexOffset; //Relative to the start of vertex data

        public bool HasValue => TotalVertexStride != 0;

        public BnfmAttribute() {
            Format = AttributeFormat.Float;
            TotalVertexStride = 44;
        }

        public BnfmAttribute(AttributeFormat format) {
            Format = format;
            TotalVertexStride = 44;
        }

        void IFileData.Read(FileReader reader)
        {
            Format = (AttributeFormat)reader.ReadInt32();
            TotalVertexStride = reader.ReadInt32();
            VertexOffset = reader.ReadUInt32();
        }

        void IFileData.Write(FileWriter writer)
        {
            writer.Write((uint)Format);
            writer.Write(TotalVertexStride);
            writer.Write(VertexOffset);
        }

        public enum AttributeFormat
        {
            Float        = 2, //Used by vertex position
            HalfSingle   = 5, //Used by tex coords
            Byte         = 10,//Used by bone indices.
            Unorm8       = 11,//Used by colors/weights
            Snorm8       = 13,//Used by normals
            Sbyte        = 20,//Used by tangents/binormals
        }
    }
}
