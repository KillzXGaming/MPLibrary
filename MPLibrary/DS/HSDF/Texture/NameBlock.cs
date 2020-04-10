using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;

namespace MPLibrary.DS
{
    public class NameBlock : IBlockSection
    {
        public string Name { get; set; }

        public void Read(HsdfFile header, FileReader reader)
        {
            reader.Seek(-4);
            uint size = reader.ReadUInt32();
            Name = reader.ReadString((int)size, true);
        }

        public void Write(HsdfFile header, FileWriter writer)
        {

        }
    }
}
