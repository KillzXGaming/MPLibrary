using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;
using OpenTK;

namespace MPLibrary.MP10
{
    public class BnfmFile
    {
        public List<BNFM_Mesh> Meshes = new List<BNFM_Mesh>();
        public List<BNFM_Bone> Bones = new List<BNFM_Bone>();
        public List<BNFM_TextureSlot> TextureSlots = new List<BNFM_TextureSlot>();
        public List<BNFM_Material> Materials = new List<BNFM_Material>();
        public List<BNFM_MaterialMapper> MaterialMappers = new List<BNFM_MaterialMapper>();

        public List<BNFM_SkeletalAnimation> SkeletalAnimations = new List<BNFM_SkeletalAnimation>();

        const int MATERIAL_SIZE = 0xEC;
        const int MATERIAL_MAPPER_SIZE = 0x3C;
        const int TEXTURE_SLOT_SIZE = 0x5C;

        const ushort Identifier = 0x5755;

        public bool Identify(System.IO.Stream stream)
        {
            using (var reader =new FileReader(stream, true)) {
                reader.SetByteOrder(true);
                return reader.ReadUInt16() == Identifier;
            }
        }

        public BnfmFile(string fileName) {
            Read(new FileReader(fileName));
        }

        public BnfmFile(System.IO.Stream stream) {
            Read(new FileReader(stream));
        }

        internal uint StringTableOffset;

        public enum ContetType
        {
            Model = 0x10, //.bnfm
            MaterialAnimation = 0x12, //.bnfmma
            TextureAnimation = 0x13, //.bnfmta
            SkeletalAnimation = 0x21, //.bnfmsa
            VisibiltyAnimation = 0x24, //.bnfmva
            CameraAnimation = 0x25, //.bnfmca
        }

        public void Read(FileReader reader) {
            reader.SetByteOrder(true);

            uint magic = reader.ReadUInt32();
            ContetType contentType = (ContetType)reader.ReadUInt32();
            uint padding = reader.ReadUInt32();

            switch (contentType)
            {
                case ContetType.Model: ReadModel(reader); break;
                case ContetType.SkeletalAnimation: ReadSkeletalAnimaion(reader); break;
                case ContetType.MaterialAnimation: ReadMaterialAnimaion(reader); break;
                case ContetType.TextureAnimation: ReadTextureAnimation(reader); break;
                case ContetType.VisibiltyAnimation: ReadVisibiltyAnimaion(reader); break;
                case ContetType.CameraAnimation: ReadCameraAnimaion(reader); break;
                default:
                    Console.WriteLine($"Unknown content type! {contentType}");
                    break;
            }
        }

        private void ReadModel(FileReader reader)
        {
            uint sectionSize = reader.ReadUInt32(); //Goes up to the faces if none are used

            uint faceDataSize = reader.ReadUInt32();
            uint vertexDataSize = reader.ReadUInt32();
            uint boneIndexTableOffset = reader.ReadUInt32();
            uint unknown = reader.ReadUInt32();
            uint vertexDataOffset = reader.ReadUInt32();
            uint faceDataOffset = reader.ReadUInt32();
            uint numRenderInfo = reader.ReadUInt32();
            uint numMeshAttributeLists = reader.ReadUInt32();
            uint numBones = reader.ReadUInt32();
            uint numMeshes = reader.ReadUInt32();
            uint numTextureSlots = reader.ReadUInt32();
            uint numMaterials = reader.ReadUInt32();
            uint numMeshInfos = reader.ReadUInt32();
            uint numSkinningIndices = reader.ReadUInt32();
            uint numSkinningBones = reader.ReadUInt32();
            uint numStrings = reader.ReadUInt32();
            uint renderInfoOffset = reader.ReadUInt32();
            uint meshAttributeListOffset = reader.ReadUInt32();
            uint boneOffset = reader.ReadUInt32();
            uint meshOffset = reader.ReadUInt32();
            uint textureSlotOffset = reader.ReadUInt32();
            uint materialOffset = reader.ReadUInt32();
            uint meshInfoOffset = reader.ReadUInt32();
            uint boneMatricesOffset = reader.ReadUInt32();
            uint boneMatricesOffset2 = reader.ReadUInt32();
            StringTableOffset = reader.ReadUInt32();

            for (int i = 0; i < numMeshAttributeLists; i++)
            {
                reader.SeekBegin(meshAttributeListOffset + (i * 128));

            }

            for (int i = 0; i < numBones; i++)
            {
                reader.SeekBegin(boneOffset + (i * 220));
                Bones.Add(new BNFM_Bone(this, reader));
            }

            for (int i = 0; i < Bones.Count; i++)
            {
                Bones[i].ParentIndex = -1;
                for (int j = 0; j < Bones.Count; j++)
                {
                    if (Bones[i].Parent == Bones[j].Name)
                        Bones[i].ParentIndex = j;
                }
            }

            List<uint> vertexCountArray = new List<uint>();
            for (int i = 0; i < numMeshes; i++)
            {
                reader.SeekBegin(meshOffset + (i * 48));

                var mesh = new BNFM_Mesh();
                mesh.Name = GetString(reader, reader.ReadUInt32());
                mesh.NameHash = reader.ReadUInt32();
                uint boneIndicesOffset = reader.ReadUInt32();
                uint polyOffset = reader.ReadUInt32();
                reader.ReadUInt32();
                reader.ReadUInt32();
                uint numFaces = reader.ReadUInt32();
                uint numVertices = reader.ReadUInt32();
                uint numSkinning = reader.ReadUInt32();
                reader.ReadUInt32();
                mesh.MaterialIndex = reader.ReadUInt32();
                reader.ReadUInt32();

                Meshes.Add(mesh);
                vertexCountArray.Add(numVertices);

                reader.SeekBegin(faceDataOffset + polyOffset);
                for (int f = 0; f < numFaces; f++)
                    mesh.Faces.Add(reader.ReadUInt16());

                if (numSkinning > 0) {
                    using (reader.TemporarySeek(boneIndicesOffset, System.IO.SeekOrigin.Begin)) {
                        mesh.BoneIndices = reader.ReadUInt32s((int)numSkinning);
                    }
                }
            }

            reader.SeekBegin(vertexDataOffset);
            foreach (var mesh in Meshes)
            {
                var numVertices = vertexCountArray[Meshes.IndexOf(mesh)];
                for (int v = 0; v < numVertices; v++)
                {
                    BNFM_Vertex vertex = new BNFM_Vertex();
                    mesh.Vertices.Add(vertex);
                    vertex.Position = new OpenTK.Vector3(
                        reader.ReadSingle(),
                        reader.ReadSingle(),
                        reader.ReadSingle());
                    Vector4 nrm = Read_8_8_8_8_Snorm(reader);
                    vertex.Normal = nrm.Xyz.Normalized();
                    vertex.Color = new OpenTK.Vector4(
                     reader.ReadByte() / 255f,
                     reader.ReadByte() / 255f,
                     reader.ReadByte() / 255f,
                     reader.ReadByte() / 255f);
                    vertex.TexCoord0 = new OpenTK.Vector2(
                        reader.ReadHalfSingle(),
                        reader.ReadHalfSingle());
                    vertex.TexCoord1 = new OpenTK.Vector2(
                        reader.ReadHalfSingle(),
                        reader.ReadHalfSingle());
                    vertex.BoneIndices = new OpenTK.Vector4(
                     reader.ReadByte(),
                     reader.ReadByte(),
                     reader.ReadByte(),
                     reader.ReadByte());
                    vertex.BoneWeights = new OpenTK.Vector4(
                      reader.ReadByte() / 255f,
                      reader.ReadByte() / 255f,
                      reader.ReadByte() / 255f,
                      reader.ReadByte() / 255f);
                    vertex.Unknown = new OpenTK.Vector2(
                        reader.ReadSingle(),
                        reader.ReadSingle());
                }
            }

            List<uint> textureSlotPositions = new List<uint>();
            if (numTextureSlots > 0)
            {
                for (int i = 0; i < numTextureSlots; i++) {
                    reader.SeekBegin(textureSlotOffset + (i * TEXTURE_SLOT_SIZE));

                    textureSlotPositions.Add((uint)reader.Position);
                    TextureSlots.Add(new BNFM_TextureSlot(this, reader));
                }
            }

            if (numMaterials > 0) {
                for (int i = 0; i < numMaterials; i++)
                {
                    reader.SeekBegin(materialOffset + (i * MATERIAL_SIZE));

                    BNFM_Material mat = new BNFM_Material();
                    Materials.Add(mat);
                    mat.Name = GetString(reader, reader.ReadUInt32());
                    mat.NameHash = reader.ReadUInt32();
                    //Parse 4 offsets of texture slots and link them accodingly
                    mat.TextureSlots[0] = GetTextureSlot(textureSlotPositions, reader.ReadUInt32());
                    mat.TextureSlots[1] = GetTextureSlot(textureSlotPositions, reader.ReadUInt32());
                    mat.TextureSlots[2] = GetTextureSlot(textureSlotPositions, reader.ReadUInt32());
                    mat.TextureSlots[3] = GetTextureSlot(textureSlotPositions, reader.ReadUInt32());
                }
            }
            if (numMeshInfos > 0) {
                for (int i = 0; i < numMeshInfos; i++)
                {
                    reader.SeekBegin(meshInfoOffset + (i * MATERIAL_MAPPER_SIZE));
                    BNFM_MaterialMapper mapper = new BNFM_MaterialMapper();
                    MaterialMappers.Add(mapper);
                    mapper.MeshName = GetString(reader, reader.ReadUInt32());
                    mapper.MeshNameHash = reader.ReadUInt32();
                    mapper.MaterialName = GetString(reader, reader.ReadUInt32());
                    mapper.MaterialNameHash = reader.ReadUInt32();
                    reader.ReadUInt32(); //1
                    reader.ReadUInt32(); //meshOffset
                    reader.ReadUInt32(); //materialOffset
                    reader.ReadUInt32(); //vertex attribute offset
                    reader.ReadUInt32(); //1
                    mapper.Radius = reader.ReadSingle(); //1
                    mapper.Offset = new Vector3(
                         reader.ReadSingle(),
                         reader.ReadSingle(),
                         reader.ReadSingle());
                    mapper.Radius = reader.ReadSingle(); //1
                    reader.ReadUInt32(); //index
                    reader.ReadUInt32(); //unk

                    foreach (var mesh in Meshes)
                    {
                        if (mesh.Name == mapper.MeshName)
                            mesh.MaterialIndex = (uint)Materials.FindIndex(x => x.Name == mapper.MaterialName);
                    }
                }
            }
        }

        private BNFM_TextureSlot GetTextureSlot(List<uint> positions, uint offset)
        {
            if (positions.Contains(offset)) {
                int index = positions.IndexOf(offset);
                return TextureSlots[index];
            }
            return null;
        }

        public static Vector4 Read_8_8_8_8_Snorm(FileReader reader)
        {
            return new Vector4(reader.ReadSByte() / 255f, reader.ReadSByte() / 255f, reader.ReadSByte() / 255f, reader.ReadSByte() / 255f);
        }

        private void ReadSkeletalAnimaion(FileReader reader)
        {
            SkeletalAnimations.Add(BNFMSA.ParseAnimations(this, reader));
        }

        private void ReadTextureAnimation(FileReader reader)
        {

        }

        private void ReadMaterialAnimaion(FileReader reader)
        {

        }

        private void ReadVisibiltyAnimaion(FileReader reader)
        {

        }

        private void ReadCameraAnimaion(FileReader reader)
        {

        }

        internal string GetString(FileReader reader, uint offset)
        {
            if (offset == uint.MaxValue)
                return string.Empty;

            using (reader.TemporarySeek( offset, System.IO.SeekOrigin.Begin)) {
                return reader.ReadZeroTerminatedString();
            }
        }
    }
}
