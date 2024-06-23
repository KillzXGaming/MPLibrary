using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using MPLibrary.MP10.IO;

namespace MPLibrary.MP10
{
    public class BnfmFile : ResourceFile
    {
        /// <summary>
        /// The list of models in the file.
        /// </summary>
        public List<BnfmModel> Models = new List<BnfmModel>();

        public BnfmFile(string fileName)
        {
            Read(new FileReader(System.IO.File.OpenRead(fileName)));
        }

        public BnfmFile(System.IO.Stream stream)
        {
            Read(new FileReader(stream));
        }

        public void Save(string fileName)
        {
            using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                Write(new FileWriter(fs));
            }
        }

        public void Save(System.IO.Stream stream)
        {
            Write(new FileWriter(stream));
        }

        public override void ReadFile(FileReader reader)
        {
            uint sectionSize = reader.ReadUInt32(); //Goes up to the faces if none are used
            uint faceBufferSize = reader.ReadUInt32(); //Total size of all face data
            uint vertexBufferSize = reader.ReadUInt32(); //Total size of all vertex data
            uint boneIndexTableOffset = reader.ReadUInt32(); //Offset to bone index table (used by meshes)
            reader.ReadUInt32(); //1
            uint vertexDataOffset = reader.ReadUInt32();
            uint faceDataOffset = reader.ReadUInt32();
            uint modelCount = reader.ReadUInt32(); //1
            uint attributeCount = reader.ReadUInt32();
            uint boneCount = reader.ReadUInt32();
            uint polyCount = reader.ReadUInt32();
            uint textureSlotCount = reader.ReadUInt32();
            uint materialCount = reader.ReadUInt32();
            uint meshCount = reader.ReadUInt32();
            uint boneMatrixCount = reader.ReadUInt32();
            uint boneMatrixSkinningCount = reader.ReadUInt32();
            uint stringCount = reader.ReadUInt32();
            uint modelOffset = reader.ReadUInt32();
            uint attributesOffset = reader.ReadUInt32();
            uint bonesOffset = reader.ReadUInt32();
            uint polyOffset = reader.ReadUInt32();
            uint textureSlotOffset = reader.ReadUInt32();
            uint matOffset = reader.ReadUInt32();
            uint meshOffset = reader.ReadUInt32();
            uint matrixTable1Offset = reader.ReadUInt32();
            uint matrixTable2Offset = reader.ReadUInt32();
            uint stringTblOffset = reader.ReadUInt32();
            reader.ReadUInt32(); //padding
            reader.ReadUInt32(); //padding

            //Only need to read the models then buffer data
            reader.Seek(modelOffset, System.IO.SeekOrigin.Begin);
            for (int i = 0; i < modelCount; i++)
            {
                BnfmModel model = new BnfmModel();
                model.Read(reader);
                Models.Add(model);
            }

            //Set vertex data
            foreach (var model in Models) {
                foreach (var mesh in model.Meshes) {
                    foreach (var poly in mesh.Polygons)
                    {
                        poly.Vertices = BnfmVertexBuffer.ReadBuffer(mesh, poly, reader, vertexDataOffset);

                        reader.Seek(faceDataOffset + poly.FaceOffset, SeekOrigin.Begin);
                        poly.Faces = reader.ReadUInt16s((int)poly.FaceCount);
                    }
                }
            }
        }

        public override void WriteFile(FileWriter writer)
        {
        }
    }
}
