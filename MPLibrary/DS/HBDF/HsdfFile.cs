using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;

namespace MPLibrary.DS
{
    public class HsdfFile
    {
        public List<ModelBlock> Models = new List<ModelBlock>();
        public List<TextureBlock> Textures = new List<TextureBlock>();

        public bool Identify(System.IO.Stream stream)
        {
            using (var reader = new FileReader(stream, true))
            {
                return reader.CheckSignature(4, "HSDF");
            }
        }

        public HsdfFile(string fileName)
        {
            Read(new FileReader(fileName));
        }

        public HsdfFile(System.IO.Stream stream)
        {
            Read(new FileReader(stream));
        }

        public static bool HasMeshes(System.IO.Stream stream)
        {
            using (var reader = new FileReader(stream, true)) {
                reader.SetByteOrder(false);
                uint magic = reader.ReadUInt32();
                uint fileSize = reader.ReadUInt32();
                while (!reader.EndOfStream)
                {
                    string sectionMagic = reader.ReadString(4, Encoding.ASCII);
                    return sectionMagic == "MDLF";
                }
            }
            return false;
        }

        public void Read(FileReader reader)
        {
            reader.SetByteOrder(false);
            uint magic = reader.ReadUInt32();
            uint fileSize = reader.ReadUInt32();
            while (!reader.EndOfStream) {
                var block = ReadBlock(reader);
                if (block is ModelBlock)
                    Models.Add((ModelBlock)block);
                if (block is TextureBlock)
                    Textures.Add((TextureBlock)block);
            }
        }

        public IBlockSection ReadBlock(FileReader reader)
        {
            string sectionMagic = reader.ReadString(4, Encoding.ASCII);
            uint sectionSize = reader.ReadUInt32();
            long pos = reader.Position;

            IBlockSection block = null;
            switch (sectionMagic)
            {
                case "MDLF": //Model
                    block = new ModelBlock(); break;
                case "OBJO": //Object block
                    block = new ObjectBlock(); break;
                case "MESH": //Mesh
                    block = new MeshBlock(); break;
                case "SKIN": //Mesh skinning
                    block = new SkinningBlock(); break;
                case "ENVS": //Skinning Envelope section
                    block = new EnvelopeBlock(); break;
                case "STRB": //String Table
                    block = new StringTable(); break;
                case "TEXS": //Texture section
                    block = new TextureBlock(); break;
                case "NAME": //Name
                    block = new NameBlock(); break;
                case "TEXO": //Texture block to map palettes to images by name
                    block = new TextureMapperBlock(); break;
                case "IMGO": //Image block
                    block = new ImageBlock(); break;
                case "PLTO": //Palette block
                    block = new PaletteDataBlock(); break;
                case "ANMF": //Animation block
                    block = new AnimationBlock(); break;
                default:
                    Console.WriteLine($"Unknown section! {sectionMagic}");
                    break;
            }

            if (block != null)
                block.Read(this, reader);

            //Keep track of the model sub sections like textures
            if (sectionMagic != "MDLF")
                reader.SeekBegin(pos + sectionSize);
            return block;
        }

        public void Save(System.IO.Stream stream)
        {

        }
    }
}
