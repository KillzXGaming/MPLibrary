using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.IO;

namespace MPLibrary.WiiU
{
    public class XB
    {
        public bool CanSave { get; set; } = false;

        public string[] Description { get; set; } = new string[] { "ND Cubed Binary XML" };
        public string[] Extension { get; set; } = new string[] { "*.xml" };

        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, System.IO.Stream stream)
        {
            using (var reader = new FileReader(stream, true))
            {
                return reader.CheckSignature(2, "XB");
            }
        }

        public BinaryXML Header;

        public void Load(System.IO.Stream stream) {
            Header = new BinaryXML(stream);
        }

        public void Save(System.IO.Stream stream) {
            Header.Save(stream);
        }
    }
}
