using System;
using System.Text;
using System.Numerics;
using System.Collections.Generic;
using System.IO;
using Syroot.BinaryData;

namespace MPLibrary.MP10.IO
{
    public class FileReader : Toolbox.Core.IO.FileReader
    {
        private Dictionary<uint, IFileData> _dataMap;

        public FileReader(Stream stream) : base(stream)
        {
            ByteOrder = ByteOrder.BigEndian;
            _dataMap = new Dictionary<uint, IFileData>();   
        }

        internal void CheckSignature(string signature)
        {
            string magic = ReadString(signature.Length, Encoding.ASCII);
            if (magic != signature)
                Console.WriteLine($"Invalid signature {magic}! Expected {signature}.");
        }

        internal string LoadString()
        {
            uint offset = ReadOffset();
            if (offset == 0) return "";

            using (TemporarySeek(offset, SeekOrigin.Begin)) {
                return ReadString(BinaryStringFormat.ZeroTerminated);
            }
        }

        internal Matrix4x4 ReadMatrix3x4()
        {
            var row1 = ReadVector4();
            var row2 = ReadVector4();
            var row3 = ReadVector4();

            return new Matrix4x4(
                row1.X, row1.Y, row1.Z, row1.W,
                row2.X, row2.Y, row2.Z, row2.W,
                row3.X, row3.Y, row3.Z, row3.W,
                0, 0, 0, 1);
        }

        internal void SavePos(IFileData data)
        {

        }

        internal T ReadSection<T>() where T : IFileData, new()
        {

            T instance = new T();

            uint offset = (uint)Position;
            if (!_dataMap.ContainsKey(offset))
                _dataMap.Add(offset, instance);

            instance.Read(this);

            return instance;
        }

        internal StringHash LoadStringHash()
        {
            return new StringHash()
            {
                Value = LoadString(),
                Hash = ReadUInt32(),
            };
        }

        internal T LoadCustom<T>(Func<T> callback, uint ofs = 0)
        {
            uint offset = ofs != 0 ? ofs : ReadOffset();
            if (offset == 0 || offset == uint.MaxValue) return default(T);

            using (TemporarySeek(offset, SeekOrigin.Begin)) {
                return callback.Invoke();
            }
        }

        internal Vector2 ReadVector2() => new Vector2(ReadSingle(), ReadSingle());

        internal Vector3 ReadVector3() => new Vector3(ReadSingle(), ReadSingle(), ReadSingle());

        internal Vector4 ReadVector4() => new Vector4(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());

        internal T Load<T>() where T : IFileData, new()
        {
            uint offset = ReadUInt32();
            if (offset == uint.MaxValue) return default(T);

            if (offset == 0) return default (T);

            if (_dataMap.ContainsKey(offset))
                return (T)_dataMap[offset];

            using (TemporarySeek(offset, SeekOrigin.Begin))
            {
                T instance = new T();
                _dataMap.Add(offset, instance);

                instance.Read(this);
                return instance;
            }
        }

        internal List<T> LoadList<T>(uint count, uint? ofs = null) where T : IFileData, new()
        {
            List<T> list = new List<T>();

            uint offset = ofs.HasValue ? ofs.Value : ReadUInt32();
            if (offset == 0) return list;

            using (TemporarySeek(offset, SeekOrigin.Begin))
            {
                for (int i = 0; i < count; i++)
                {
                    T instance = new T();
                    instance.Read(this);
                    list.Add(instance);
                }
                return list;
            }
        }

        internal string ReadSignature() => ReadString(4, Encoding.ASCII);

        /// <summary>
        /// Reads the offset from a relative position into an absoule value.
        /// </summary>
        internal uint ReadOffset()
        {
            return ReadUInt32();
        }

        internal Matrix4x4 ReadMatrix4()
        {
            return new Matrix4x4(
                ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(),
                ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(),
                ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(),
                ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
        }

        /// <summary>
        /// Seeks the given offset from the start of the stream.
        /// </summary>
        internal void SeekBegin(long value) => Seek(value, SeekOrigin.Begin);
    }
}
