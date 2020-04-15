using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;

namespace MPLibrary.DS
{
    public class LZBIN
    {
        public List<FileEntry> Files = new List<FileEntry>();

        public LZBIN(System.IO.Stream stream, string FileName)
        {
            using (var reader = new FileReader(stream))
            {
                Read(FileName, reader);
            }
        }

        void Read(string FileName, FileReader reader)
        {
            reader.SetByteOrder(false);
            uint numFiles = reader.ReadUInt32();
            for (int i = 0; i < numFiles; i++)
            {
                reader.SeekBegin(4 + (i * 8));
                uint offset = reader.ReadUInt32();
                uint size = reader.ReadUInt32();

                reader.SeekBegin(offset + 4);
                var file = new FileEntry();
                file.FileData = LZ77.Decompress(reader.ReadBytes((int)size));
                Files.Add(file);
            }
            UpdateFileNames(FileName);
        }


        private void UpdateFileNames(string FileName)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(FileName);
            for (int i = 0; i < Files.Count; i++)
            {
                Files[i].FileName = $"{name}{string.Format("{0:00}", i)}";

                using (var fileReader = new FileReader(Files[i].FileData))
                {
                    fileReader.SetByteOrder(true);
                    string magic = fileReader.ReadString(4, Encoding.ASCII);
                    if (magic == "HBDF")
                    {
                        Files[i].FileName = $"{Files[i].FileName}.hbdf";
                        Files[i].ImageKey = "model";
                    }
                    else
                        Files[i].FileName = $"{Files[i].FileName}.dat";
                }
            }
        }

        public void Save(System.IO.Stream stream)
        {
            using (var writer = new FileWriter(stream))
            {
                writer.SetByteOrder(false);
                writer.Write(Files.Count);
                for (int i = 0; i < Files.Count; i++)
                {
                    writer.Write(uint.MaxValue);
                    writer.Write(Files[i].FileData.Length);
                }

                for (int i = 0; i < Files.Count; i++)
                {
                    writer.WriteUint32Offset(4 + (i * 8), 4);
                    writer.Write(Files[i].FileData);
                }
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