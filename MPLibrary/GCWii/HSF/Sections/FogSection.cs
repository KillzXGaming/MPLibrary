using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Library.IO;
using System.Runtime.InteropServices;

namespace MPLibrary
{
    public class FogSection : HSFSection
    {
        public byte[] data;

        public override void Read(FileReader reader, HsfFile header) {
            data = reader.ReadBytes(16);
        }

        public override void Write(FileWriter writer, HsfFile header) {
            writer.Write(data);
        }
    }   

}
