using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using OpenTK;

namespace MPLibrary.GCN
{
    public class Board
    {
        public List<MPSpace> Spaces = new List<MPSpace>();
       
        public Board() { }

        public Board(Stream stream, GameVersion gameVersion)
        {
            using (var reader = new FileReader(stream)) {
                Read(reader, gameVersion);
            }
        }

        private void Read(FileReader reader, GameVersion gameVersion)
        {
            reader.SetByteOrder(true);
            uint numSpaces = reader.ReadUInt32();
            for (int i = 0; i < numSpaces; i++)
            {
                MPSpace space = new MPSpace();
                Spaces.Add(space);
                space.Position = reader.ReadVec3();
                space.EulerRotation = reader.ReadVec3();
                space.Scale = reader.ReadVec3();
                space.Param1 = reader.ReadUInt16();
                space.Param2 = reader.ReadUInt16();
                if (gameVersion > GameVersion.MP5)
                    space.Param3 = reader.ReadUInt16();
                space.TypeID = reader.ReadUInt16();
                ushort numLinks = reader.ReadUInt16();
                space.ChildrenIndices = reader.ReadUInt16s(numLinks).ToList();
            }
        }

        public void Save(Stream stream, GameVersion gameVersion)
        {
            using (var writer = new FileWriter(stream)) {
                Write(writer, gameVersion);
            }
        }

        public void Write(FileWriter writer, GameVersion gameVersion)
        {
            writer.SetByteOrder(true);
            writer.Write(Spaces.Count);
            foreach (var space in Spaces)
            {
                writer.Write(space.Position);
                writer.Write(space.EulerRotation);
                writer.Write(space.Scale);
                writer.Write(space.Param1);
                writer.Write(space.Param2);
                if (gameVersion > GameVersion.MP5)
                    writer.Write(space.Param3);
                writer.Write(space.TypeID);
                writer.Write((ushort)space.ChildrenIndices.Count);
                writer.Write(space.ChildrenIndices);
            }
        }
    }

    public class MPSpace
    {
        public Vector3 Position { get; set; }
        public Vector3 EulerRotation { get; set; }
        public Vector3 Scale { get; set; }

        public List<ushort> ChildrenIndices { get; set; } = new List<ushort>();

        public ushort Param1 { get; set; }
        public ushort Param2 { get; set; }
        public ushort Param3 { get; set; }
        public ushort TypeID { get; set; }
    }
}
