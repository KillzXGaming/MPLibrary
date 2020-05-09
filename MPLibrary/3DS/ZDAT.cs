using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;

namespace MPLibrary.ThreeDS
{
    public class ZDAT
    {
        public List<FileEntry> Files = new List<FileEntry>();

        public uint Version { get; set; }

        public ZDAT(System.IO.Stream stream) {
            using (var reader = new FileReader(stream)) {
                Read(reader);
            }
        }

        void Read(FileReader reader)
        {
            reader.SetByteOrder(false);
            reader.ReadSignature(4, "RZPK");
            Version = reader.ReadUInt32();
            uint numFiles = reader.ReadUInt32();
            uint dataOffset = reader.ReadUInt32();
            uint dataSize = reader.ReadUInt32();
            reader.Seek(0x0C);

            for (int i = 0; i < numFiles; i++)
            {
                reader.SeekBegin(32 + (i * 44));
                var file = new FileEntry();
                file.FileName = reader.ReadString(0x20, true);
                uint decompressedSize = reader.ReadUInt32();
                uint size = reader.ReadUInt32();
                uint offset = reader.ReadUInt32();

                reader.SeekBegin(dataOffset + offset);
                file.FileData = STLibraryCompression.ZLIB.Decompress(reader.ReadBytes((int)size));
                Files.Add(file);
            }
        }

        public class FileEntry
        {
            public string FileName { get; set; }

            public byte[] FileData { get; set; }

            public string ImageKey { get; set; }
        }
    }
}
