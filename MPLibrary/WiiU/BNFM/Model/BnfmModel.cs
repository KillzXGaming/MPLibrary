using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPLibrary.MP10.IO;

namespace MPLibrary.MP10
{
    public class BnfmModel
    {
        /// <summary>
        /// Gets or sets the model name.
        /// </summary>
        public StringHash ModelName { get; set; } = new StringHash("");

        /// <summary>
        /// Gets or sets the mesh list of the model.
        /// </summary>
        public List<BnfmMesh> Meshes = new List<BnfmMesh>();

        /// <summary>
        /// Gets or sets the bone list of the model.
        /// </summary>
        public List<BnfmBone> Bones = new List<BnfmBone>();

        /// <summary>
        /// An unknown value.
        /// </summary>
        public uint Unknown = 1;

        /// <summary>
        /// Array of unknown values.
        /// </summary>
        public float[] Values = new float[9];

        public BnfmModel()
        {
        }

        internal void Read(FileReader reader)
        {
            ModelName = reader.LoadStringHash();
            uint bonesOffset = reader.ReadUInt32(); //bone offset
            uint polyOffset = reader.ReadUInt32(); //poly offset
            uint matOffset = reader.ReadUInt32(); //mat offset
            uint meshOffset = reader.ReadUInt32(); //mesh offset

            uint boneCount = reader.ReadUInt32(); //bone offset
            uint polyCount = reader.ReadUInt32(); //poly offset
            uint matCount = reader.ReadUInt32(); //mat offset
            uint meshCount = reader.ReadUInt32(); //mesh offset

            Unknown = reader.ReadUInt32();
            Values = reader.ReadSingles(9);

            //Read only meshes/bones. They connect everything together
            if (meshCount > 0)
            {
                reader.Seek(meshOffset, System.IO.SeekOrigin.Begin);
                for (int i = 0; i < meshCount; i++)
                    Meshes.Add(reader.ReadSection<BnfmMesh>());
            }
            if (boneCount > 0)
            {
                reader.Seek(bonesOffset, System.IO.SeekOrigin.Begin);
                for (int i = 0; i < boneCount; i++)
                    Bones.Add(reader.ReadSection<BnfmBone>());
            }
        }

        internal void Write(FileWriter writer)
        {
            writer.Write(ModelName);
            writer.SaveList(Bones);//bone offset
            writer.SaveList(Meshes.SelectMany(x => x.Polygons)); //poly offset
            writer.SaveList(Meshes.Select(x => x.Material));//mat offset
            writer.SaveList(Meshes); //mesh offset

            writer.Write(Bones.Count); //bone count
            writer.Write(Meshes.Sum(x => x.Polygons.Count)); //poly count
            writer.Write(Meshes.Select(x => x.Material).Distinct().ToList().Count); //mat count
            writer.Write(Meshes.Count); //mesh count

            writer.Write(Unknown);
            writer.Write(Values);
        }
    }
}
