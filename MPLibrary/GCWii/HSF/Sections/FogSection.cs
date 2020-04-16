using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STLibrary.IO;
using System.Runtime.InteropServices;
using OpenTK;

namespace MPLibrary.GCN
{
    public class FogSection : HSFSection
    {
        public Vector4 ColorStart; //Often 76,76,76,0
        public Vector4 ColorEnd;

        public float Start { get; set; }
        public float End { get; set; }

        public override void Read(FileReader reader, HsfFile header) {
            ColorStart = new Vector4(
                reader.ReadByte(), reader.ReadByte(),
                reader.ReadByte(), reader.ReadByte());
            Start = reader.ReadSingle();
            End = reader.ReadSingle();
            ColorEnd = new Vector4(
                reader.ReadByte(), reader.ReadByte(),
                reader.ReadByte(), reader.ReadByte());
        }

        public override void Write(FileWriter writer, HsfFile header) { 
            writer.Write((byte)ColorStart.X);
            writer.Write((byte)ColorStart.Y);
            writer.Write((byte)ColorStart.Z);
            writer.Write((byte)ColorStart.W);
            writer.Write(Start);
            writer.Write(End);
            writer.Write((byte)ColorEnd.X);
            writer.Write((byte)ColorEnd.Y);
            writer.Write((byte)ColorEnd.Z);
            writer.Write((byte)ColorEnd.W);
        }
    }   

}
