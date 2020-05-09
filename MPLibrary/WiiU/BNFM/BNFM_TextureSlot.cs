using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using OpenTK;

namespace MPLibrary.MP10
{
    public class BNFM_TextureSlot
    {
        public string Name { get; set; }
        public uint NameHash { get; set; }

        public BNFM_TextureSlot(BnfmFile header, FileReader reader)
        {
            Name = header.GetString(reader, reader.ReadUInt32());
            NameHash = reader.ReadUInt32();
        }
    }
}
