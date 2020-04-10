using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;
using System.Runtime.InteropServices;

namespace MPLibrary
{
    public class SymbolDataSection : HSFSection
    {
        public int[] SymbolIndices;

        public override void Read(FileReader reader, HsfFile header) {
            SymbolIndices = reader.ReadInt32s((int)this.Count);
        }

        public override void Write(FileWriter writer, HsfFile header) {
            writer.Write(SymbolIndices);
        }
    }

}
