using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using MPLibrary.MP10.IO;

namespace MPLibrary.MP10
{
    public class BnfmPolygon : ListItem, IFileData
    {
        /// <summary>
        /// The name of the polygon.
        /// </summary>
        public StringHash Name { get; set; } = new StringHash("");

        /// <summary>
        /// The vertex data used by the polygon.
        /// </summary>
        public BnfmVertex[] Vertices { get; set; } = new BnfmVertex[0];


        /// <summary>
        /// The face data used by the polygon.
        /// </summary>
        public ushort[] Faces { get; set; }

        /// <summary>
        /// An array of bone indices used to remap the vertex indices into the skeleton bone list.
        /// </summary>
        public uint[] BoneIndices { get; set; }

        /// <summary>
        /// The total amount of skinning used on a rigged model.
        /// </summary>
        public uint SkinningCount { get; set; }

        /// <summary>
        /// Determines if any bone indices use inverse matrices (smooth) or single bind
        /// </summary>
        public SkinningType SkinningMethod { get; set; } = SkinningType.SingleBind;

        /// <summary>
        /// An unknown value. Always 0?
        /// </summary>
        public uint Unknown2 { get; set; } = 0;

        //Usage for reading the buffer

        /// <summary>
        /// The amount of faces used in the face buffer.
        /// </summary>
        internal uint FaceCount { get; private set; }
        /// <summary>
        /// The amount of vertices used in the face buffer.
        /// </summary>
        internal uint VertexCount { get; set; }
        /// <summary>
        /// The offset to the face in the buffer
        /// </summary>
        internal uint FaceOffset { get; set; }

        void IFileData.Read(FileReader reader)
        {
            Name = reader.LoadStringHash();
            uint boneIndicesOffset = reader.ReadUInt32();
            FaceOffset = reader.ReadUInt32();
            SkinningMethod = (SkinningType)reader.ReadUInt32();
            Unknown2 = reader.ReadUInt32();
            FaceCount = reader.ReadUInt32();
            VertexCount = reader.ReadUInt32();
            uint boneIndicesCount = reader.ReadUInt32();
            SkinningCount = reader.ReadUInt32();
            Index = reader.ReadInt32();
            Flag = reader.ReadInt32();

            BoneIndices = reader.LoadCustom(() => reader.ReadUInt32s((int)boneIndicesCount), boneIndicesOffset);
        }

        private long boneIndicesPos;

        void IFileData.Write(FileWriter writer)
        {
            writer.Write(Name);

            FaceCount = (ushort)this.Faces.Length;

            boneIndicesPos = writer.Position;
            writer.Write(0); //write later
            writer.Write(FaceOffset);
            writer.Write((uint)SkinningMethod);
            writer.Write(Unknown2);
            writer.Write(FaceCount);
            writer.Write(VertexCount);
            writer.Write(BoneIndices.Length);
            writer.Write(SkinningCount);
            writer.Write(Index);
            writer.Write(Flag);
        }

        internal void SaveBoneIndicesOffset(FileWriter writer)
        {
            long pos = writer.Position;
            using (writer.TemporarySeek(boneIndicesPos, System.IO.SeekOrigin.Begin)) {
                writer.Write((uint)pos);
            }
        }

        public enum SkinningType
        {
            SingleBind,
            HasSkinning,
        }
    }
}
