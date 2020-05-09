using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.IO;

namespace MPLibrary.GCN
{
    public class MessFile : IFileFormat
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { "Mario Party GCN Message" };
        public string[] Extension { get; set; } = new string[] { "*.bin", "*.dat" };

        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, System.IO.Stream stream)
        {
            return fileInfo.FileName == "board.dat" ||
                   fileInfo.FileName == "mini.dat" ||
                   fileInfo.FileName == "mini_e.dat" ||
                   fileInfo.FileName == "board_e.dat";
        }

        MessFileData messFile;

        public void Load(System.IO.Stream stream) {
            messFile = new MessFileData(stream, Encoding.UTF8);
        }

        public void Save(System.IO.Stream stream) {
            messFile.Save(stream, messFile.Version, Encoding.UTF8);
        }
    }
}
