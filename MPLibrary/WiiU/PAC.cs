using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;

namespace MPLibrary
{
    public class PAC
    {
        public List<FileEntry> Files = new List<FileEntry>();

        public PAC(string fileName) {
            Read(new FileReader(fileName));
        }

        public PAC(System.IO.Stream stream) {
            Read(new FileReader(stream));
        }

        public void Read(FileReader reader)
        {
            reader.SetByteOrder(true);

            uint signature = reader.ReadUInt32();
            uint headerSize = reader.ReadUInt32();
            reader.ReadUInt32(); //padding
            uint fileInfoSize = reader.ReadUInt32();
            uint totalFileSize = reader.ReadUInt32();
            uint languageSize = reader.ReadUInt32();
            uint unknown1 = reader.ReadUInt32();
            uint unknown2 = reader.ReadUInt32();
            uint numFiles = reader.ReadUInt32();
            reader.ReadUInt32();
            reader.ReadUInt32();
            reader.ReadUInt32();
            reader.ReadUInt32();
            uint languageOffset = reader.ReadUInt32();
            uint fileInfoOffset = reader.ReadUInt32();
            uint stringTableOffset = reader.ReadUInt32();
            uint firstFileOffset = reader.ReadUInt32();

            reader.SeekBegin(fileInfoOffset);
            for (int i = 0; i < numFiles; i++)
            {
                uint fileNameOffset = reader.ReadUInt32();
                uint fileNameHash = reader.ReadUInt32();
                uint fileExtOffset = reader.ReadUInt32();
                uint fileExtHash = reader.ReadUInt32();
                uint dataOffset = reader.ReadUInt32();
                uint dataSize = reader.ReadUInt32();
                uint compressedSize = reader.ReadUInt32();
                uint compressedSize2 = reader.ReadUInt32();
                uint padding1 = reader.ReadUInt32();
                uint padding2 = reader.ReadUInt32();
                uint compressionFlags = reader.ReadUInt32();
                uint padding3 = reader.ReadUInt32();

                string fileName = GetString(reader, fileNameOffset);
                string ext = GetString(reader, fileExtOffset);

                using (reader.TemporarySeek(dataOffset, System.IO.SeekOrigin.Begin)) {
                    byte[] data = reader.ReadBytes((int)compressedSize);

                    Files.Add(new FileEntry()
                    {
                        Compressed = dataSize != compressedSize,
                        FileName = $"{fileName}",
                        Data = data,
                    });
                }
            }
        }

        private string GetString(FileReader reader, uint offset)
        {
            using (reader.TemporarySeek(offset, System.IO.SeekOrigin.Begin)) {
                return reader.ReadZeroTerminatedString();
            }
        }

        public class FileEntry
        {
            public byte[] Data { get; set; }

            public string FileName { get; set; }
            public bool Compressed { get; set; }
        }
    }
}
