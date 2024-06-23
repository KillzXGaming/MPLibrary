using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.IO;
using Syroot.BinaryData;

namespace MPLibrary.MP10.IO
{
    public class FileWriter : Toolbox.Core.IO.FileWriter
    {
        Dictionary<string, List<OffsetEntry>> SavedStrings = new Dictionary<string, List<OffsetEntry>>();
        internal Dictionary<long, IFileData> SavedSections = new Dictionary<long, IFileData>();
        internal Dictionary<long, IFileData> WrittenSections = new Dictionary<long, IFileData>();

        internal Dictionary<long, IEnumerable<IFileData>> SavedLists = new Dictionary<long, IEnumerable<IFileData>>();
        internal Dictionary<long, IEnumerable<IFileData>> WrittenLists = new Dictionary<long, IEnumerable<IFileData>>();

        Dictionary<string, SavedOffset> SavedOffsets = new Dictionary<string, SavedOffset>();

        public int GetStringCount() => SavedStrings.Count;

        public FileWriter(Stream stream) : base(stream)
        {
            ByteOrder = ByteOrder.BigEndian;
            SavedStrings.Add("", new List<OffsetEntry>());
        }

        internal bool TryGetItemEntry(object data, out long position)
        {
            foreach (var savedItem in SavedSections)
            {
                if (savedItem.Value.Equals(data))
                {
                    position = savedItem.Key;
                    return true;
                }
            }
            position = 0;
            return false;
        }

        internal void AssertSize(long pos, int size)
        {
            if (Position - pos != size)
                Console.WriteLine("Bad size!");
        }

        internal void WriteSignature(string value)
        {
            Write(value, BinaryStringFormat.NoPrefixOrTermination);
        }

        internal void WriteList(IEnumerable<IFileData> data)
        {
            AlignBytes(16);

            int index = 0;
            foreach (var section in data)
            {
                if (section is IListIndex)
                    ((IListIndex)section).Index = index;

                //Allocate offset
                WrittenSections.Add(Position, section);
                section.Write(this);

                index++;
            }
        }

        internal void SetupOffsets()
        {
            foreach (var entry in WrittenSections)
            {
                foreach (var saved in SavedLists)
                {
                    if (saved.Value.FirstOrDefault() == entry.Value)
                        WriteOffsetValue(entry.Key, saved.Key);
                }
                foreach (var saved in SavedSections)
                {
                    if (saved.Value == entry.Value)
                        WriteOffsetValue(entry.Key, saved.Key);
                }
            }
        }

        private void WriteOffsetValue(long dest, long source)
        {
            using (TemporarySeek(source, SeekOrigin.Begin))
            {
                Write((uint)dest);
            }
        }

        /// <summary>
        /// Aligns the data by writing bytes (rather than seeking)
        /// </summary>
        /// <param name="alignment"></param>
        /// <param name="value"></param>
        internal void AlignBytes(int alignment, byte value = 0)
        {
            var startPos = Position;
            long position = Seek((-Position % alignment + alignment) % alignment, SeekOrigin.Current);

            Seek(startPos, System.IO.SeekOrigin.Begin);
            while (Position != position)
            {
                Write(value);
            }
        }

        internal void Write(Vector2 value)
        {
            Write(value.X);
            Write(value.Y);
        }

        internal void Write(Vector3 value)
        {
            Write(value.X);
            Write(value.Y);
            Write(value.Z);
        }

        internal void Write(Vector4 value)
        {
            Write(value.X);
            Write(value.Y);
            Write(value.Z);
            Write(value.W);
        }

        internal void Write(Matrix4x4 value)
        {
            Write(value.M11);
            Write(value.M12);
            Write(value.M13);
            Write(value.M14);

            Write(value.M21);
            Write(value.M22);
            Write(value.M23);
            Write(value.M24);

            Write(value.M31);
            Write(value.M32);
            Write(value.M33);
            Write(value.M34);

            Write(value.M41);
            Write(value.M42);
            Write(value.M43);
            Write(value.M44);
        }

        internal void SaveString(string value)
        {
            long pos = Position;
            Write(0);

            if (value == null) value = "";

            //Save to the table to be written later
            if (!SavedStrings.ContainsKey(value))
                SavedStrings.Add(value, new List<OffsetEntry>());

            SavedStrings[value].Add(new OffsetEntry()
            {
                Value = pos,
            });
        }

        internal void Save(IFileData value, int index = 0)
        {
            if (value == null)
            {
                Write(uint.MaxValue);
                return;
            }

            SavedSections.Add(Position, value);
            Write(uint.MaxValue);        
        }

        public void WriteOffset()
        {

        }

        public long SaveOffset()
        {
            long pos = Position;
            Write(uint.MaxValue);
            return pos;
        }

        internal void SaveList(IEnumerable<IFileData> list)
        {
            if (list?.Count() == 0)
            {
                Write(0);
                return;
            }

            var pos = Position;
            //Section to save later
            if (!SavedSections.ContainsKey(pos))
                SavedLists.Add(pos, list);

            Write(uint.MaxValue);
        }

        internal void WriteOffset(long ofsPosition, long startAddress = 0)
        {
            var offset = BaseStream.Position - startAddress;
            using (TemporarySeek(ofsPosition, SeekOrigin.Begin)) {
                Write((int)offset);
            }
        }

        internal void WriteSectionLength(long startPos, int lengthOfs = 0)
        {
            var length = (uint)(BaseStream.Position - startPos);
            using (TemporarySeek(startPos + lengthOfs, SeekOrigin.Begin)) {
                Write(length);
            }
        }

        internal void Write(StringHash stringHash)
        {
            //Calculate the hash before saving
            stringHash.CalculateHash();

            SaveString(stringHash.Value);
            Write(stringHash.Hash);
        }

        internal void WriteStringTable()
        {
            //string pool sorted in ACII order
            var sorted = SavedStrings.Keys.ToArray();
            //    Array.Sort(sorted, StringComparer.Ordinal);

            foreach (var str in sorted)
            {
                //Save the position for linking offset
                long pos = Position;
                //Zero derminated string
                Write(str, BinaryStringFormat.ZeroTerminated);

                //Prepare all the offsets linked to the string
                var offsets = SavedStrings[str];
                foreach (var ofs in offsets)
                {
                    using (TemporarySeek(ofs.Value, SeekOrigin.Begin)) {
                        Write((uint)(pos - ofs.StartAddress));
                    }
                }
            }
        }

        public void WriteLength(long dest, long size)
        {
            uint length = (uint)(size);
            using (TemporarySeek(dest, SeekOrigin.Begin)) {
                Write(length);
            }

        }

        public void Write(IFileData fileData)
        {
            if (fileData == null)
                return;

            fileData.Write(this);
        }

        internal void SaveData(IFileData data, long ofsOffsetPos, long startOffset)
        {
            long pos = Position;
            if (TryGetItemEntry(data, out long posOfs))
            {
                using (TemporarySeek(ofsOffsetPos, System.IO.SeekOrigin.Begin)) {
                    Write((uint)(posOfs - startOffset));
                }
            }
            else
            {
                WriteOffset(ofsOffsetPos, startOffset);
                SavedSections.Add(pos, data);
                data.Write(this);
            }
        }

        class SavedOffset
        {
            public List<long> SectionSources;
            public long Position;
        }

        class OffsetEntry
        {
            public long Value;
            public long StartAddress;
        }
    }
}
