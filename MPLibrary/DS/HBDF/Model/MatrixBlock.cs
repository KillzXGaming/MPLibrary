using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;

namespace MPLibrary.DS
{
    public class MatrixBlock
    {
        public uint Index { get; set; }
        public float TranslateX;
        public float TranslateY;
        public float TranslateZ;
        public int RotateX;
        public int RotateY;
        public int RotateZ;
        public float ScaleX;
        public float ScaleY;
        public float ScaleZ;

        public MatrixBlock(FileReader reader)
        {
            Index = reader.ReadUInt32();
            TranslateX = reader.ReadSingleInt();
            TranslateY = reader.ReadSingleInt();
            TranslateZ = reader.ReadSingleInt();
            RotateX = reader.ReadInt32();
            RotateY = reader.ReadInt32();
            RotateZ = reader.ReadInt32();
            ScaleX = reader.ReadSingleInt();
            ScaleY = reader.ReadSingleInt();
            ScaleZ = reader.ReadSingleInt();
        }
    }
}
