using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox;
using Toolbox.Library;
using Toolbox.Library.IO;

namespace FirstPlugin
{
    public class MPBIN 
    {
        public bool Identify(string fileName, System.IO.Stream stream)
        {
            if (Utils.GetExtension(fileName) == ".bin")
            {
                return true;
            }
            return false;
        }

        public List<FileEntry> files = new List<FileEntry>();

        public enum CompressionType
        {
            None = 0,
            LZSS = 1,
            SLIDE = 2,
            FSLIDE_Alt = 3,
            FLIDE = 4,
            RLE = 5,
            INFLATE = 7,
        }

        public void Load(System.IO.Stream stream)
        {
            using (var reader = new FileReader(stream))
            {
                reader.SetByteOrder(true);
                uint numFiles = reader.ReadUInt32();
                uint[] offsets = reader.ReadUInt32s((int)numFiles);

                for (int i = 0; i < numFiles; i++)
                {
                    reader.SeekBegin(offsets[i]);
                    uint compSize = 0;
                    if (i < numFiles - 1)
                        compSize = offsets[i + 1] - offsets[i] - 8;
                    else
                        compSize = (uint)reader.BaseStream.Length - offsets[i] - 8;

                    var file = new FileEntry();
                    file.DecompressedSize = reader.ReadUInt32();
                    file.CompressionType = (CompressionType)reader.ReadUInt32();
                    file.FileData = reader.ReadBytes((int)compSize);
                    file.FileName = $"File_{i}";
                    files.Add(file);

                    switch (file.CompressionType)
                    {
                        case CompressionType.LZSS:
                            file.FileData = DecompressLZSS(file.FileData, (int)file.DecompressedSize);
                            break;
                        case CompressionType.SLIDE:
                        case CompressionType.FLIDE:
                        case CompressionType.FSLIDE_Alt:
                            file.FileData = DecompressSlide(file.FileData, (int)file.DecompressedSize);
                            break;
                        case CompressionType.RLE:
                            file.FileData = DecompressRLE(file.FileData, (int)file.DecompressedSize);
                            break;
                        case CompressionType.INFLATE:
                            file.FileData = STLibraryCompression.ZLIB.Decompress(Utils.SubArray(file.FileData, 8));
                            break;
                    }

                    using (var fileReader = new FileReader(file.FileData))
                    {
                        fileReader.SetByteOrder(true);
                        string magic = fileReader.ReadString(4, Encoding.ASCII);
                        if (magic == "HSFV")
                            file.FileName = $"File_{i}.hsf";

                        if (fileReader.BaseStream.Length > 16)
                        {
                            fileReader.SeekBegin(12);
                            uint offset = fileReader.ReadUInt32();
                            if (offset == 20)
                                file.FileName = $"File_{i}.atb";
                        }
                    }
                }
            }
        }

        public void Unload()
        {

        }

        public void Save(System.IO.Stream stream)
        {
            using (var writer = new FileWriter(stream)) {
                writer.SetByteOrder(true);
                writer.Write(files.Count);
                writer.Write(new uint[files.Count]); //reserve space for offsets
                for (int i = 0; i < files.Count; i++)
                {
                    writer.WriteUint32Offset(4 + (i * 4));
                    writer.Write(files[i].FileData.Length);
                    writer.Write((uint)files[i].CompressionType);
                    byte[] savedBytes = files[i].FileData;
                    switch (files[i].CompressionType)
                    {
                        case CompressionType.LZSS:
                            break;
                        case CompressionType.INFLATE:
                            savedBytes = STLibraryCompression.ZLIB.Compress(files[i].FileData);
                            break;
                    }
                    writer.Write(savedBytes);
                }
            }
        }

        public bool AddFile(ArchiveFileInfo archiveFileInfo)
        {
            return false;
        }

        public bool DeleteFile(ArchiveFileInfo archiveFileInfo)
        {
            return false;
        }

        public class FileEntry : ArchiveFileInfo
        {
            public CompressionType CompressionType { get; set; }
            public uint DecompressedSize { get; set; }
        }

        //From https://github.com/Sage-of-Mirrors/HoneyBee/blob/master/HoneyBee/src/archive/Compression.cs#L78
        private static byte[] DecompressLZSS(byte[] compressed_data, int uncompressed_size)
        {
            int WINDOW_SIZE = 1024;
            int WINDOW_START = 0x3BE;
            int MIN_MATCH_LEN = 3;

            int src_offset = 0;
            int dest_offset = 0;
            int window_offset = WINDOW_START;

            byte[] dest = new byte[uncompressed_size];
            byte[] window_buffer = new byte[WINDOW_SIZE];

            ushort cur_code_byte = 0;

            while (dest_offset < uncompressed_size)
            {
                if ((cur_code_byte & 0x100) == 0)
                {
                    cur_code_byte = compressed_data[src_offset++];
                    cur_code_byte |= 0xFF00;
                }

                if ((cur_code_byte & 0x001) == 1)
                {
                    dest[dest_offset] = compressed_data[src_offset];
                    window_buffer[window_offset] = compressed_data[src_offset];

                    src_offset++;
                    dest_offset++;

                    window_offset = (window_offset + 1) % WINDOW_SIZE;
                }

                else
                {
                    byte byte1 = compressed_data[src_offset++];
                    byte byte2 = compressed_data[src_offset++];

                    int offset = ((byte2 & 0xC0) << 2) | byte1;
                    int length = (byte2 & 0x3F) + MIN_MATCH_LEN;

                    byte val = 0;
                    for (int i = 0; i < length; i++)
                    {
                        val = window_buffer[offset % WINDOW_SIZE];
                        window_buffer[window_offset] = val;

                        window_offset = (window_offset + 1) % WINDOW_SIZE;
                        dest[dest_offset] = val;

                        dest_offset++;
                        offset++;
                    }
                }

                cur_code_byte >>= 1;
            }


            return dest;
        }

        private static byte[] DecompressRLE(byte[] compressed_data, int uncompressed_size)
        {
            int dest_offset = 0;
            int src_offset = 0;
            int code_byte = 0;
            byte repeat_length;
            int i;

            byte[] dest = new byte[uncompressed_size];
            while (dest_offset < uncompressed_size)
            {
                code_byte = compressed_data[src_offset];
                src_offset++;
                repeat_length = (byte)(code_byte & 0x7F);

                if ((code_byte & 0x80) != 0)
                {
                    i = 0;
                    while (i < repeat_length)
                    {
                        dest[dest_offset] = compressed_data[src_offset];
                        dest_offset++;
                        src_offset++;
                        i++;
                    }
                }
                else
                {
                    byte repeated_byte = compressed_data[src_offset];
                    src_offset++;

                    i = 0;
                    while (i < repeat_length)
                    {
                        dest[dest_offset] = repeated_byte;
                        dest_offset++;
                        i++;
                    }
                }
            }
            return dest;
        }

        //From https://github.com/gamemasterplc/mpbintools/blob/master/bindump.c#L240
        private static byte[] DecompressSlide(byte[] compressed_data, int uncompressed_size)
        {
            int src_offset = 4;
            int dest_offset = 0;
            int code_word = 0;
            int num_code_word_bits_left = 0;
            int i = 0;

            byte[] dest = new byte[uncompressed_size];

            while (dest_offset < uncompressed_size)
            {
                if (num_code_word_bits_left == 0)
                {
                    code_word = ReadBigEndian(compressed_data, src_offset);
                    src_offset += 4;
                    num_code_word_bits_left = 32;
                }

                if ((code_word & 0x80000000) != 0)
                {
                    dest[dest_offset] = compressed_data[src_offset];
                    src_offset++;
                    dest_offset++;
                }
                else
                {
                    //Interpret Next 2 Bytes as a Backwards Distance and Length
                    byte byte1 = compressed_data[src_offset++];
                    byte byte2 = compressed_data[src_offset++];

                    int dist_back = (((byte1 & 0x0F) << 8) | byte2) + 1;
                    int copy_length = ((byte1 & 0xF0) >> 4) + 2;

                    //Special Case Where the Upper 4 Bits of byte1 are 0
                    if (copy_length == 2)
                    {
                        copy_length = compressed_data[src_offset++] + 18;
                    }

                    byte value;
                    i = 0;

                    while (i < copy_length && dest_offset < uncompressed_size)
                    {
                        if (dist_back > dest_offset)
                        {
                            value = 0;
                        }
                        else
                        {
                            value = dest[dest_offset - dist_back];
                        }
                        dest[dest_offset] = value;
                        dest_offset++;
                        i++;
                    }
                }
                code_word = code_word << 1;
                num_code_word_bits_left--;
            }

            return dest;
        }

        private static int ReadBigEndian(byte[] data, int offset)
        {
            int value = data[offset + 0] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8 | data[offset + 3];
            return value;
        }
    }
}
