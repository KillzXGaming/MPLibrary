using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using OpenTK;

namespace MPLibrary.GCN
{
    public class Board
    {
        public List<Space> Spaces = new List<Space>();

        public Board(FileReader reader, GameVersion gameVersion)
        {
            reader.SetByteOrder(true);
            uint numSpaces = reader.ReadUInt32();
            for (int i = 0; i < numSpaces; i++)
            {
                Space space = new Space();
                Spaces.Add(space);
                space.Position = reader.ReadVec3();
                space.EulerRotation = reader.ReadVec3();
                space.Scale = reader.ReadVec3();
                space.Unk1 = reader.ReadUInt16();
                space.Unk2 = reader.ReadUInt16();
                if (gameVersion > GameVersion.MP5)
                    space.Unk3 = reader.ReadUInt16();
                space.TypeID = reader.ReadUInt16();
                ushort numLinks = reader.ReadUInt16();
                space.ChildrenIndices = reader.ReadUInt16s(numLinks).ToList();
            }
        }
    }

    public class Space
    {
        public Vector3 Position { get; set; }
        public Vector3 EulerRotation { get; set; }
        public Vector3 Scale { get; set; }

        public List<ushort> ChildrenIndices { get; set; }

        public ushort Unk1 { get; set; }
        public ushort Unk2 { get; set; }
        public ushort Unk3 { get; set; }
        public ushort TypeID { get; set; }
    }
}
