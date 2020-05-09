using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.IO;

namespace MPLibrary.GCN
{
    public class ATB : IFileFormat
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { "Mario Party GCN Sprite" };
        public string[] Extension { get; set; } = new string[] { "*.atb" };

        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, System.IO.Stream stream)
        {
            return Utils.GetExtension(fileInfo.FileName) == ".atb";
        }

        AtbFile AtbFile;

        public void Load(System.IO.Stream stream) {
            AtbFile = new AtbFile(stream);
        }

        public void Save(System.IO.Stream stream) {
            AtbFile.Save(stream);
        }
    }
}
