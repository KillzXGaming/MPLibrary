using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using Toolbox.Core;

namespace MPLibrary.DS
{
    public class LZBIN : IFileFormat, IArchiveFile
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { "Mario Party DS Archive" };
        public string[] Extension { get; set; } = new string[] { "*.hsf" };

        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, System.IO.Stream stream)
        {
            if (stream.Length < 16)
                return false;

            if (fileInfo.Extension == ".bin")
            {
                using (var reader = new FileReader(stream, true))
                {
                    reader.SetByteOrder(false);
                    reader.Position = 0;
                    uint count = reader.ReadUInt32();
                    uint offset = reader.ReadUInt32();
                    if (offset == (8 * count))
                        return true;
                }
            }
            return false;
        }

        public bool CanAddFiles { get; set; } = true;
        public bool CanRenameFiles { get; set; }
        public bool CanReplaceFiles { get; set; } = true;
        public bool CanDeleteFiles { get; set; } = true;

        public void ClearFiles() { files.Clear(); }

        public IEnumerable<ArchiveFileInfo> Files => files;
        public List<FileEntry> files = new List<FileEntry>();

        public LZBIN() { }

        public LZBIN(string FileName)
        {
            using (var stream = new System.IO.FileStream(FileName, System.IO.FileMode.Open, System.IO.FileAccess.Read)) {
                Read(FileName, stream);
            }
        }

        public void Load(System.IO.Stream stream) {
            Read(FileInfo.FileName, stream);
        }

        void Read(string FileName, System.IO.Stream stream)
        {
            using (var reader = new FileReader(stream))
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
                    file.SetData(LZ77.Decompress(reader.ReadBytes((int)size)));
                    files.Add(file);
                }
            }

            UpdateFileNames(FileName);
        }

        public bool AddFile(ArchiveFileInfo archiveFileInfo)
        {
            files.Add(new FileEntry()
            {
                FileData = archiveFileInfo.FileData,
            });
            UpdateFileNames(FileInfo.FileName);

            return false;
        }

        public bool DeleteFile(ArchiveFileInfo archiveFileInfo)
        {
            files.Remove((FileEntry)archiveFileInfo);
            return true;
        }

        private void UpdateFileNames(string FileName)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(FileName);
            for (int i = 0; i < files.Count; i++)
            {
                files[i].FileName = $"{name}{string.Format("{0:00}", i)}";

                using (var fileReader = new FileReader(files[i].FileData))
                {
                    fileReader.SetByteOrder(true);
                    string magic = fileReader.ReadString(4, Encoding.ASCII);
                    if (magic == "HBDF")
                    {
                        files[i].FileName = $"{files[i].FileName}.hbdf";
                        if (HsdfFile.HasMeshes(files[i].FileData))
                        {
                            files[i].ImageKey = "model";
                            files[i].FileName = $"Models/{files[i].FileName}.hbdf";
                        }
                        else
                            files[i].FileName = $"Animations/{files[i].FileName}.hbdf";
                    }
                    else
                        files[i].FileName = $"{files[i].FileName}.dat";
                }
            }
        }

        public void Save(System.IO.Stream stream)
        {
            using (var writer = new FileWriter(stream))
            {
                writer.SetByteOrder(false);
                writer.Write(files.Count);
                for (int i = 0; i < files.Count; i++)
                {
                    writer.Write(uint.MaxValue);
                    writer.Write(files[i].FileData.Length);
                }

                for (int i = 0; i < files.Count; i++)
                {
                    writer.WriteUint32Offset(4 + (i * 8), 4);
                    writer.Write(files[i].AsBytes());
                }
            }
        }

        public class FileEntry : ArchiveFileInfo
        {
            public string ImageKey { get; set; }
        }
    }
}