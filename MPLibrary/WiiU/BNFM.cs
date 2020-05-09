using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.IO;

namespace MPLibrary.MP10
{
    public class BNFM : IFileFormat
    {
        public bool CanSave { get; set; } = false;

        public string[] Description { get; set; } = new string[] { "ND Cubed Resource" };
        public string[] Extension { get; set; } = new string[] { "*.bnfm" };

        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, System.IO.Stream stream)
        {
            using (var reader = new FileReader(stream, true))
            {
                reader.SetByteOrder(true);
                return reader.ReadUInt16() == 0x5755;
            }
        }

        public BnfmFile Header;

        public void Load(System.IO.Stream stream) {
            Header = new BnfmFile(stream);
        }

        public void Save(System.IO.Stream stream) {
            Header.Save(stream);
        }
    }
}
