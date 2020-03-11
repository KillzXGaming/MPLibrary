using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ComponentModel;
using Toolbox.Library.IO;
using System.Text.RegularExpressions;

namespace Toolbox.Library
{
    public enum ArchiveFileState
    {
        Empty = 0,
        Archived = 1,
        Added = 2,
        Replaced = 4,
        Renamed = 8,
        Deleted = 16
    }

    /// <summary>
    /// A common archive format interface used to edit archive file formats
    /// </summary>
    public interface IArchiveFile
    {
        bool CanAddFiles { get; } 
        bool CanRenameFiles { get; } 
        bool CanReplaceFiles { get; } 
        bool CanDeleteFiles { get; }

        IEnumerable<ArchiveFileInfo> Files { get; }

        void ClearFiles();
        bool AddFile(ArchiveFileInfo archiveFileInfo);
        bool DeleteFile(ArchiveFileInfo archiveFileInfo);
    }

    public class ArchiveFileInfo 
    {
        // Opens the file format automatically (may take longer to open the archive file)
        [Browsable(false)]
        public virtual bool OpenFileFormatOnLoad { get; set; }

        [Browsable(false)]
        // The source file. If an archive is in another archive, this is necessary to get the original path
        public string SourceFile { get; internal set; }

        [Browsable(false)]
        public string ImageKey { get; set; }

        [Browsable(false)]
        public string SelectedImageKey { get; set; }

        [Browsable(false)]
        public FileType FileDataType = FileType.Default;

        //Wether or not to check the file magic to determine the type
        //This sets the icons if there's no proper extension, and may add more special operations
        //This should be disabled on larger archives!
        [Browsable(false)]
        public virtual bool CheckFileMagic { get; set; } = false;

        //Properties to show for the archive file when selected
        [Browsable(false)]
        public virtual object DisplayProperties { get; set; }

        [Browsable(false)]
        public virtual bool CanLoadFile { get; set; } = true;

        [Browsable(false)]
        public virtual Dictionary<string, string> ExtensionImageKeyLookup { get; }

        public virtual string FileSize { get {return STMath.GetFileSize(
            FileDataStream != null ? FileDataStream.Length : FileData.Length, 4); } }

        [Browsable(false)]
        public IFileFormat FileFormat = null; //Format attached for saving

        [Browsable(false)]
        private byte[] _fileData = null;

        //Full File Name
        private string _fileName = string.Empty;

        [Browsable(false)]
        public string FileName
        {
            get
            {
                return _fileName;
            }
            set
            {
                _fileName = value;
            }
        }

        public static void SaveFileFormat(ArchiveFileInfo archiveFile, IFileFormat fileFormat)
        {
            if (fileFormat != null && fileFormat.CanSave)
            {
                if (archiveFile.FileDataStream != null)
                {
                    var mem = new System.IO.MemoryStream();
                    fileFormat.Save(mem);
                    archiveFile.FileDataStream = mem;
                    //Reload file data
                    fileFormat.Load(archiveFile.FileDataStream);
                }
                else
                {
                    var mem = new System.IO.MemoryStream();
                    fileFormat.Save(mem);
                }
            }
        }

        public void SaveFileFormat()
        {
            if (FileFormat != null && FileFormat.CanSave)
            {
                if (FileDataStream != null)
                {
                    Console.WriteLine($"Updating FileDataStream " + (FileDataStream is FileStream));
                    if (FileDataStream is FileStream)
                        FileDataStream.Close();

                    var mem = new System.IO.MemoryStream();
                    FileFormat.Save(mem);
                    FileDataStream = mem;
                    //Reload file data
                    FileFormat.Load(FileDataStream);
                }
                else
                {
                    var mem = new System.IO.MemoryStream();
                    FileFormat.Save(mem);
                }
            }
        }

        [Browsable(false)]
        public string Name { get; set; } = string.Empty; //File Name (No Path)

        [Browsable(false)]
        public virtual byte[] FileData
        {
            get { return _fileData; }
            set { _fileData = value; }
        }

        public virtual Stream FileDataStream
        {
            get
            {
                if (_fileStream != null)
                    _fileStream.Position = 0;

                return _fileStream;
            }
            set { _fileStream = value; }
        }

        protected Stream _fileStream = null;

        [Browsable(false)]
        public ArchiveFileState State { get; set; } = ArchiveFileState.Empty;
    }
}
