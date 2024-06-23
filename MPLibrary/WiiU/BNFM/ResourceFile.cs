using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPLibrary.MP10.IO;

namespace MPLibrary.MP10
{
    public class ResourceFile
    {
        /// <summary>
        /// Represents the magic used by the source file.
        /// Value is always 0x5755.
        /// </summary>
        public uint Magic { get; set; } = 0x5755;

        /// <summary>
        /// Represents the content type of the resource.
        /// </summary>
        public ContetType Type { get; set; } = ContetType.Model;

        public void Read(FileReader reader)
        {
            Magic = reader.ReadUInt32();
            uint headerSize = reader.ReadUInt32();
            Type = (ContetType)reader.ReadUInt32();
            ReadFile(reader);
        }

        public void Write(FileWriter writer)
        {
            writer.Write(Magic);
            writer.Write(16);
            writer.Write((uint)Type);
            WriteFile(writer);
        }

        public virtual void ReadFile(FileReader reader)
        {

        }

        public virtual void WriteFile(FileWriter writer)
        {

        }

        public enum ContetType
        {
            Model = 0x10, //.bnfm
            MaterialAnimation = 0x12, //.bnfmma
            TextureAnimation = 0x13, //.bnfmta
            SkeletalAnimation = 0x21, //.bnfmsa
            VisibiltyAnimation = 0x24, //.bnfmva
            CameraAnimation = 0x25, //.bnfmca
        }
    }
}
